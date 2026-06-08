using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

/// <summary>Lists DIAL deployments via the OpenAI-compatible <c>/openai/deployments</c> endpoint.</summary>
public interface IDialDeployments
{
    /// <summary>Gets the deployments from <c>/openai/deployments</c>.</summary>
    Task<DialDeploymentList> GetOpenAiAsync(CancellationToken cancellationToken = default);
}
