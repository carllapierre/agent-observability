using System.Diagnostics;
using SimpleAgent.Core.Telemetry.Constants;
using SimpleAgent.Core.Telemetry.Models;

namespace SimpleAgent.Core.Telemetry.Services;

/// <summary>
/// Langfuse-specific telemetry implementation.
/// Extends base telemetry with Langfuse observation types and attributes.
/// </summary>
public class LangfuseTelemetry : AgentTelemetry
{
    public override TelemetryScope StartTrace(string name, string? sessionId = null, string? userId = null, string[]? tags = null, object? input = null)
    {
        var scope = base.StartTrace(name, sessionId, userId, tags, input);
        var activity = Activity.Current;

        if (activity is not null)
        {
            activity.SetTag(LangfuseAttributes.TraceName, name);

            if (sessionId is not null)
                activity.SetTag(LangfuseAttributes.SessionId, sessionId);

            if (userId is not null)
                activity.SetTag(LangfuseAttributes.UserId, userId);

            if (tags is { Length: > 0 })
                activity.SetTag(LangfuseAttributes.TraceTags, string.Join(",", tags));
        }

        return scope;
    }

    public override TelemetryScope StartSpan(string name, object? input = null) =>
        StartLangfuseObservation(name, LangfuseObservationTypes.Span, input);

    public override TelemetryScope StartEvent(string name, object? input = null) =>
        StartLangfuseObservation(name, LangfuseObservationTypes.Event, input);

    public override TelemetryScope StartAgent(string name, object? input = null) =>
        StartLangfuseObservation(name, LangfuseObservationTypes.Agent, input);

    public override TelemetryScope StartChain(string name, object? input = null) =>
        StartLangfuseObservation(name, LangfuseObservationTypes.Chain, input);

    public override TelemetryScope StartRetriever(string name, object? input = null) =>
        StartLangfuseObservation(name, LangfuseObservationTypes.Retriever, input);

    public override TelemetryScope StartEvaluator(string name, object? input = null) =>
        StartLangfuseObservation(name, LangfuseObservationTypes.Evaluator, input);

    public override TelemetryScope StartEmbedding(string name, object? input = null) =>
        StartLangfuseObservation(name, LangfuseObservationTypes.Embedding, input);

    public override TelemetryScope StartGuardrail(string name, object? input = null) =>
        StartLangfuseObservation(name, LangfuseObservationTypes.Guardrail, input);

    public override TelemetryScope StartTool(string toolName, object? inputs = null)
    {
        var scope = base.StartTool(toolName, inputs);
        Activity.Current?.SetTag(LangfuseAttributes.ObservationType, LangfuseObservationTypes.Tool);
        return scope;
    }

    public override GenerationScope StartGeneration(GenerationContext context)
    {
        var scope = base.StartGeneration(context);
        Activity.Current?.SetTag(LangfuseAttributes.ObservationType, LangfuseObservationTypes.Generation);
        return scope;
    }

    private TelemetryScope StartLangfuseObservation(string name, string observationType, object? input = null)
    {
        var scope = StartObservation(name, input);
        Activity.Current?.SetTag(LangfuseAttributes.ObservationType, observationType);
        return scope;
    }
}
