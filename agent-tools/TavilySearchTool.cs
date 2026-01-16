using System.Text;
using System.Text.Json;
using AgentCore.Settings;
using AgentCore.Tools.Attributes;

namespace AgentTools;

/// <summary>
/// Tool that performs web searches using the Tavily API.
/// Configure by setting TavilySearchTool.Settings before use.
/// </summary>
[Tool("web_search", Description = "Performs a web search to find current information, news, or facts using Tavily")]
public static class TavilySearchTool
{
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// Static settings - must be configured before use.
    /// </summary>
    public static TavilySettings Settings { get; set; } = new();

    public static string Execute(
        [ToolParameter("The search query to find information for")] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return "Error: query parameter is required";

            if (string.IsNullOrEmpty(Settings.ApiKey))
                return "Error: Tavily API key is not configured. Set TavilySearchTool.Settings or add to appsettings.json";

            // Build the payload
            var payload = new
            {
                api_key = Settings.ApiKey,
                query = query,
                search_depth = Settings.SearchDepth,
                include_answer = Settings.IncludeAnswer,
                max_results = Settings.MaxResults
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
