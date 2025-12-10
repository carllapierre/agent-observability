using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleAgent.Core.DependencyInjection.Attributes;

namespace SimpleAgent.Core.DependencyInjection.Extensions;

/// <summary>
/// Extension methods for automatic service registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds configuration and auto-registers services from the assembly containing the marker type.
    /// Scans for [ConfigSection] and [RegisterKeyed] attributes.
    /// Returns the configuration for further use.
    /// </summary>
    public static IConfiguration AddServicesFromAssembly<TAssemblyMarker>(this IServiceCollection services)
    {
        var assembly = typeof(TAssemblyMarker).Assembly;
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddConfigSections(assembly, configuration);
        services.AddKeyedServices(assembly);

        return configuration;
    }

    private static void AddConfigSections(this IServiceCollection services, Assembly assembly, IConfiguration configuration)
    {
        var settingsTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetCustomAttribute<ConfigSectionAttribute>() != null);

        foreach (var settingsType in settingsTypes)
        {
            var attr = settingsType.GetCustomAttribute<ConfigSectionAttribute>()!;
            var section = configuration.GetSection(attr.SectionName);

            var configureMethod = typeof(OptionsConfigurationServiceCollectionExtensions)
                .GetMethods()
                .First(m => m.Name == "Configure" &&
                           m.GetParameters().Length == 2 &&
                           m.GetParameters()[1].ParameterType == typeof(IConfiguration));

            var genericMethod = configureMethod.MakeGenericMethod(settingsType);
            genericMethod.Invoke(null, [services, section]);
        }
    }

    private static void AddKeyedServices(this IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var implementationType in types)
        {
            var attributes = implementationType.GetCustomAttributes()
                .Where(a => a.GetType().IsGenericType &&
                           a.GetType().GetGenericTypeDefinition() == typeof(RegisterKeyedAttribute<>));

            foreach (var attribute in attributes)
            {
                var attrType = attribute.GetType();
                var serviceType = attrType.GetGenericArguments()[0];
                var key = (string)attrType.GetProperty("Key")!.GetValue(attribute)!;
                var lifetime = (Attributes.ServiceLifetime)attrType.GetProperty("Lifetime")!.GetValue(attribute)!;

                var descriptor = lifetime switch
                {
                    Attributes.ServiceLifetime.Singleton => ServiceDescriptor.KeyedSingleton(serviceType, key, implementationType),
                    Attributes.ServiceLifetime.Scoped => ServiceDescriptor.KeyedScoped(serviceType, key, implementationType),
                    Attributes.ServiceLifetime.Transient => ServiceDescriptor.KeyedTransient(serviceType, key, implementationType),
                    _ => throw new ArgumentOutOfRangeException()
                };

                services.Add(descriptor);
            }
        }
    }
}
