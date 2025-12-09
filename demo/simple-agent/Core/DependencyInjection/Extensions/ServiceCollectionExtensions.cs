using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SimpleAgent.Core.DependencyInjection.Attributes;

namespace SimpleAgent.Core.DependencyInjection.Extensions;

/// <summary>
/// Extension methods for automatic service registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans the assembly containing the specified type for classes decorated with
    /// RegisterKeyedAttribute and registers them as keyed services.
    /// </summary>
    public static IServiceCollection AddKeyedServicesFromAssembly<TAssemblyMarker>(
        this IServiceCollection services)
    {
        return services.AddKeyedServicesFromAssembly(typeof(TAssemblyMarker).Assembly);
    }

    /// <summary>
    /// Scans the specified assembly for classes decorated with RegisterKeyedAttribute
    /// and registers them as keyed services.
    /// </summary>
    public static IServiceCollection AddKeyedServicesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var implementationType in types)
        {
            // Find all RegisterKeyed<T> attributes on this type
            var attributes = implementationType.GetCustomAttributes()
                .Where(a => a.GetType().IsGenericType &&
                           a.GetType().GetGenericTypeDefinition() == typeof(RegisterKeyedAttribute<>));

            foreach (var attribute in attributes)
            {
                var attrType = attribute.GetType();
                var serviceType = attrType.GetGenericArguments()[0];
                var key = (string)attrType.GetProperty("Key")!.GetValue(attribute)!;
                var lifetime = (Attributes.ServiceLifetime)attrType.GetProperty("Lifetime")!.GetValue(attribute)!;

                // Register the keyed service
                var descriptor = lifetime switch
                {
                    Attributes.ServiceLifetime.Singleton => ServiceDescriptor.KeyedSingleton(
                        serviceType, key, implementationType),
                    Attributes.ServiceLifetime.Scoped => ServiceDescriptor.KeyedScoped(
                        serviceType, key, implementationType),
                    Attributes.ServiceLifetime.Transient => ServiceDescriptor.KeyedTransient(
                        serviceType, key, implementationType),
                    _ => throw new ArgumentOutOfRangeException()
                };

                services.Add(descriptor);
            }
        }

        return services;
    }
}
