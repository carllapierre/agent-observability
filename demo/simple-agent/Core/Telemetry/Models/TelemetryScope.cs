using System.Diagnostics;
using System.Text.Json;

namespace SimpleAgent.Core.Telemetry.Models;

/// <summary>
/// Disposable wrapper around an Activity that handles automatic closure
/// and provides methods for setting output and recording errors.
/// </summary>
public class TelemetryScope : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected readonly Activity? Activity;
    private bool _disposed;

    public TelemetryScope(Activity? activity)
    {
        Activity = activity;
    }

    /// <summary>
    /// Sets the output attribute on the span.
    /// </summary>
    /// <param name="output">The output object to serialize and record.</param>
    public void SetOutput(object? output)
    {
        if (output is null) return;

        var json = output is string s ? s : JsonSerializer.Serialize(output, JsonOptions);
        Activity?.SetTag("output", json);
    }

    /// <summary>
    /// Records an exception on the span and sets error status.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    public void RecordException(Exception exception)
    {
        Activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        Activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        }));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Activity?.Dispose();
        GC.SuppressFinalize(this);
    }
}
