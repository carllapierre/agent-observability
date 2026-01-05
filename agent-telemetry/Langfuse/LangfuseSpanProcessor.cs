using System.Diagnostics;
using AgentTelemetry.Constants;
using OpenTelemetry;

namespace AgentTelemetry.Langfuse;

/// <summary>
/// Span processor that maps GenAI semantic convention attributes to Langfuse-specific attributes.
/// This allows the core telemetry to remain provider-agnostic while still supporting Langfuse's
/// observation types and graph visualization.
/// </summary>
public class LangfuseSpanProcessor : BaseProcessor<Activity>
{
    /// <summary>
    /// Maps GenAI operation names to Langfuse observation types.
    /// Official GenAI names are mapped to Langfuse equivalents.
    /// </summary>
    private static readonly Dictionary<string, string> OperationToLangfuseType = new()
    {
        // Official GenAI semantic convention values → Langfuse types
        [OperationNames.Chat] = LangfuseObservationTypes.Generation,
        [OperationNames.InvokeAgent] = LangfuseObservationTypes.Agent,
        [OperationNames.ExecuteTool] = LangfuseObservationTypes.Tool,
        [OperationNames.Embeddings] = LangfuseObservationTypes.Embedding,
        [OperationNames.TextCompletion] = LangfuseObservationTypes.Generation,
        [OperationNames.GenerateContent] = LangfuseObservationTypes.Generation,
        
        // Custom operation names → Langfuse types (1:1 mapping)
        [OperationNames.Span] = LangfuseObservationTypes.Span,
        [OperationNames.Event] = LangfuseObservationTypes.Event,
        [OperationNames.Chain] = LangfuseObservationTypes.Chain,
        [OperationNames.Retriever] = LangfuseObservationTypes.Retriever,
        [OperationNames.Evaluator] = LangfuseObservationTypes.Evaluator,
        [OperationNames.Guardrail] = LangfuseObservationTypes.Guardrail,
    };

    public override void OnEnd(Activity activity)
    {
        // Map gen_ai.operation.name -> langfuse.observation.type
        var operationName = activity.GetTagItem(GenAIAttributes.GenAi.OperationName)?.ToString();
        if (operationName is not null)
        {
            var langfuseType = OperationToLangfuseType.GetValueOrDefault(operationName, operationName);
            activity.SetTag(LangfuseAttributes.ObservationType, langfuseType);
        }

        // Map generic trace attributes to Langfuse-specific ones
        MapAttribute(activity, GenAIAttributes.Trace.Name, LangfuseAttributes.TraceName);
        MapAttribute(activity, GenAIAttributes.Session.Id, LangfuseAttributes.SessionId);
        MapAttribute(activity, GenAIAttributes.User.Id, LangfuseAttributes.UserId);
        MapAttribute(activity, GenAIAttributes.Trace.Tags, LangfuseAttributes.TraceTags);

        base.OnEnd(activity);
    }

    private static void MapAttribute(Activity activity, string sourceKey, string targetKey)
    {
        var value = activity.GetTagItem(sourceKey);
        if (value is not null)
        {
            activity.SetTag(targetKey, value);
        }
    }
}

/// <summary>
/// Langfuse observation types for graph rendering.
/// </summary>
internal static class LangfuseObservationTypes
{
    public const string Span = "span";
    public const string Event = "event";
    public const string Generation = "generation";
    public const string Agent = "agent";
    public const string Tool = "tool";
    public const string Chain = "chain";
    public const string Retriever = "retriever";
    public const string Evaluator = "evaluator";
    public const string Embedding = "embedding";
    public const string Guardrail = "guardrail";
}

