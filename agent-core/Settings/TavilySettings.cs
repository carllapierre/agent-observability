namespace AgentCore.Settings;

/// <summary>
/// Configuration settings for Tavily search API.
/// </summary>
public class TavilySettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SearchDepth { get; set; } = "basic";
    public int MaxResults { get; set; } = 3;
    public bool IncludeAnswer { get; set; } = true;
}
