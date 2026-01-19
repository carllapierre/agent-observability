using System.Text;
using System.Text.Json;
using AgentCore.Settings;
using AgentCore.Tools.Attributes;
using AgentTelemetry.Interfaces;

namespace SimpleAgent.Tools;

/// <summary>
/// Tool that performs web searches using the Tavily API.
/// Includes built-in retriever telemetry for observability.
/// </summary>
[Tool("web_search", Description = "Performs a web search to find current information, news, or facts using Tavily")]
public class TavilySearchTool
{
    private static readonly HttpClient _httpClient = new();
    
    private readonly TavilySettings _settings;
    private readonly IAgentTelemetry? _telemetry;

    public TavilySearchTool(TavilySettings settings, IAgentTelemetry? telemetry = null)
    {
        _settings = settings;
        _telemetry = telemetry;
    }

    public string Execute(
        [ToolParameter("The search query to find information for")] string query)
    {
        // Wrap execution with retriever span if telemetry is configured
        if (_telemetry != null)
        {
            using var retriever = _telemetry.StartRetriever("Web Search", new { query });
            var result = ExecuteSearch(query);
            retriever.SetOutput(result);
            return result;
        }

        return ExecuteSearch(query);
    }

    private string ExecuteSearch(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return "Error: query parameter is required";

            if (string.IsNullOrEmpty(_settings.ApiKey))
                return "Error: Tavily API key is not configured";

            // Build the payload
            var payload = new
            {
                api_key = _settings.ApiKey,
                query = query,
                search_depth = _settings.SearchDepth,
                include_answer = _settings.IncludeAnswer,
                max_results = _settings.MaxResults
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            // Execute Request (Sync-over-Async to match signature)
            var response = _httpClient.PostAsync("https://api.tavily.com/search", content)
                                      .GetAwaiter().GetResult();

            response.EnsureSuccessStatusCode();
            var jsonStr = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            // Parse and Format
            return FormatResults(jsonStr);
        }
        catch (Exception ex)
        {
            return $"Error performing search: {ex.Message}";
        }
    }

    private static string FormatResults(string jsonStr)
    {
        using var doc = JsonDocument.Parse(jsonStr);
        var root = doc.RootElement;
        var sb = new StringBuilder();

        // Add the direct AI answer if available
        if (root.TryGetProperty("answer", out var answer) && !string.IsNullOrWhiteSpace(answer.GetString()))
        {
            sb.AppendLine("### Direct Answer");
            sb.AppendLine(answer.GetString());
            sb.AppendLine();
        }

        // Add the search results
        if (root.TryGetProperty("results", out var results))
        {
            sb.AppendLine("### Search Results");
            foreach (var result in results.EnumerateArray())
            {
                var title = result.GetProperty("title").GetString();
                var url = result.GetProperty("url").GetString();
                var text = result.GetProperty("content").GetString();

                sb.AppendLine($"* **{title}** ({url})");
                sb.AppendLine($"  {text}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
