using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleAgent.Core.DependencyInjection.Attributes;

namespace SimpleAgent.Core.DependencyInjection.Extensions;

/// <summary>
/// Extension methods for configuration binding.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds configuration from appsettings files and auto-binds all settings classes
    /// decorated with [ConfigSection] attribute.
    /// </summary>
    public static IServiceCollection AddConfiguration<TAssemblyMarker>(this IServiceCollection services)
    {
        return services.AddConfiguration(typeof(TAssemblyMarker).Assembly);
    }

    /// <summary>
    /// Adds configuration from appsettings files and auto-binds all settings classes
    /// decorated with [ConfigSection] attribute from the specified assembly.
    /// </summary>
    public static IServiceCollection AddConfiguration(this IServiceCollection services, Assembly assembly)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        // Register IConfiguration so it can be injected directly
        services.AddSingleton<IConfiguration>(configuration);

        // Find all classes with [ConfigSection] attribute and bind them
        var settingsTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetCustomAttribute<ConfigSectionAttribute>() != null);

        foreach (var settingsType in settingsTypes)
        {
            var attr = settingsType.GetCustomAttribute<ConfigSectionAttribute>()!;
            var section = configuration.GetSection(attr.SectionName);

            // Use reflection to call services.Configure<T>(section)
            var configureMethod = typeof(OptionsConfigurationServiceCollectionExtensions)
                .GetMethods()
                .First(m => m.Name == "Configure" &&
                           m.GetParameters().Length == 2 &&
                           m.GetParameters()[1].ParameterType == typeof(IConfiguration));

            var genericMethod = configureMethod.MakeGenericMethod(settingsType);
            genericMethod.Invoke(null, [services, section]);
        }

        return services;
    }
}
