namespace SimpleAgent.Core.DependencyInjection.Attributes;

/// <summary>
/// Marks a settings class for automatic configuration binding.
/// The section name in appsettings.json will be used to bind this class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ConfigSectionAttribute : Attribute
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public string SectionName { get; }

    public ConfigSectionAttribute(string sectionName)
    {
        SectionName = sectionName;
    }
}
