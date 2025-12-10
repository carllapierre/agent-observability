using System.Diagnostics;
using System.Text.Json;
using SimpleAgent.Core.Telemetry.Constants;
using SimpleAgent.Core.Telemetry.Interfaces;
using SimpleAgent.Core.Telemetry.Models;

namespace SimpleAgent.Core.Telemetry.Services;

/// <summary>
/// Base implementation of agent telemetry using standard OpenTelemetry semantics.
/// Provider-agnostic - uses only GenAI semantic conventions.
/// </summary>
public class AgentTelemetry : IAgentTelemetry
{
    private static readonly ActivitySource ActivitySource = new(GenAIAttributes.SourceName);
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public virtual TelemetryScope StartTrace(string name, string? sessionId = null, string? userId = null, string[]? tags = null, object? input = null)
    {
        var activity = ActivitySource.StartActivity(name, ActivityKind.Server);
        SetInput(activity, input);
        return new TelemetryScope(activity);
    }

    public virtual TelemetryScope StartSpan(string name, object? input = null) =>
        StartObservation(name, input);

    public virtual TelemetryScope StartEvent(string name, object? input = null) =>
        StartObservation(name, input);

    public virtual TelemetryScope StartAgent(string name, object? input = null) =>
        StartObservation(name, input);

    public virtual TelemetryScope StartChain(string name, object? input = null) =>
        StartObservation(name, input);

    public virtual TelemetryScope StartRetriever(string name, object? input = null) =>
        StartObservation(name, input);

    public virtual TelemetryScope StartEvaluator(string name, object? input = null) =>
        StartObservation(name, input);

    public virtual TelemetryScope StartEmbedding(string name, object? input = null) =>
        StartObservation(name, input);

    public virtual TelemetryScope StartGuardrail(string name, object? input = null) =>
        StartObservation(name, input);

    public virtual TelemetryScope StartTool(string toolName, object? inputs = null)
    {
        var activity = ActivitySource.StartActivity($"Tool: {toolName}", ActivityKind.Client);
        
        if (activity is not null)
        {
            activity.SetTag("tool.name", toolName);
            SetInput(activity, inputs);
        }

        return new TelemetryScope(activity);
    }

    public virtual GenerationScope StartGeneration(GenerationContext context)
    {
        var activity = ActivitySource.StartActivity($"Generation: {context.Model}", ActivityKind.Client);

        if (activity is not null)
        {
            activity.SetTag(GenAIAttributes.GenAi.System, context.Provider);
            activity.SetTag(GenAIAttributes.GenAi.RequestModel, context.Model);
            
            if (context.Input is not null)
            {
                var inputJson = SerializeInput(context.Input);
                activity.SetTag(GenAIAttributes.GenAi.Prompt, inputJson);
                activity.SetTag("input", inputJson);
            }

            if (context.Temperature.HasValue)
                activity.SetTag(GenAIAttributes.GenAi.Temperature, context.Temperature.Value);

            if (context.MaxTokens.HasValue)
                activity.SetTag(GenAIAttributes.GenAi.MaxTokens, context.MaxTokens.Value);

            if (context.TopP.HasValue)
                activity.SetTag(GenAIAttributes.GenAi.TopP, context.TopP.Value);
        }

        return new GenerationScope(activity);
    }

    public void RecordException(Exception exception)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        }));
    }

    protected TelemetryScope StartObservation(string name, object? input = null)
    {
        var activity = ActivitySource.StartActivity(name, ActivityKind.Internal);
        SetInput(activity, input);
        return new TelemetryScope(activity);
    }

    protected void SetInput(Activity? activity, object? input)
    {
        if (activity is not null && input is not null)
            activity.SetTag("input", SerializeInput(input));
    }

    protected string SerializeInput(object input) =>
        input is string s ? s : JsonSerializer.Serialize(input, JsonOptions);
}
