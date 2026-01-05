namespace SimpleAgent.Core.DependencyInjection.Attributes;

/// <summary>
/// Marks a class for automatic registration as a keyed service.
/// </summary>
/// <typeparam name="TService">The service interface type to register as.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class RegisterKeyedAttribute<TService> : Attribute where TService : class
{
    /// <summary>
    /// The key used to identify this implementation.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The service lifetime. Defaults to Singleton.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    public RegisterKeyedAttribute(string key, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        Key = key;
        Lifetime = lifetime;
    }
}

/// <summary>
/// Service lifetime options for keyed registration.
/// </summary>
public enum ServiceLifetime
{
    Singleton,
    Scoped,
    Transient
}
