namespace SimpleAgent.Core.Tools.Models;

/// <summary>
/// Runtime representation of a tool that can be called by the AI model.
/// </summary>
public record ToolDescriptor(
    string Name,
    string Description,
    IReadOnlyList<ToolParameterDescriptor> Parameters
);

/// <summary>
/// Describes a parameter of a tool.
/// </summary>
public record ToolParameterDescriptor(
    string Name,
    string Type,
    string Description,
    bool IsRequired,
    object? DefaultValue = null
);

