namespace Dial.Sharp;

/// <summary>Configuration for <see cref="DialClient"/>.</summary>
public sealed class DialClientOptions
{
    public string ChatApiVersion { get; set; } = "2024-10-21";

    public string EmbeddingsApiVersion { get; set; } = "2023-12-01-preview";

    public string AudioApiVersion { get; set; } = "2024-10-21";

    public TimeSpan NetworkTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public int MaxRetries { get; set; } = 3;
}