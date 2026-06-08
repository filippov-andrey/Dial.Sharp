using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

/// <summary>Lists DIAL applications via the OpenAI-compatible <c>/openai/applications</c> endpoint.</summary>
public interface IDialApplications
{
    /// <summary>Gets the applications from <c>/openai/applications</c>.</summary>
    Task<DialApplicationList> GetOpenAiAsync(CancellationToken cancellationToken = default);
}
