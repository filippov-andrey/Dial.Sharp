using System.Text.Json;

namespace Dial.Sharp.Rest;

/// <summary>Lists DIAL toolsets via <c>/openai/toolsets</c>.</summary>
public interface IDialToolsets
{
    /// <summary>Gets the toolsets from <c>/openai/toolsets</c> as raw JSON.</summary>
    Task<JsonElement> GetOpenAiAsync(CancellationToken cancellationToken = default);
}
