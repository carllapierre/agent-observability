namespace SimpleAgent.Core.Tools.Attributes;

/// <summary>
/// Marks a class as a tool that can be called by the AI model.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ToolAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; set; } = string.Empty;

    public ToolAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Marks a parameter with a description for the AI model.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class ToolParameterAttribute : Attribute
{
    public string Description { get; }

    public ToolParameterAttribute(string description)
    {
        Description = description;
    }
}

