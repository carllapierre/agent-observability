using SimpleAgent.Core.DependencyInjection.Attributes;

namespace SimpleAgent.Providers.Prompt;

/// <summary>
/// Langfuse connection settings.
/// </summary>
[ConfigSection("Langfuse")]
public class LangfuseSettings
{
    /// <summary>
    /// Langfuse public key (pk-lf-...).
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Langfuse secret key (sk-lf-...).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Langfuse base URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://cloud.langfuse.com";
}

