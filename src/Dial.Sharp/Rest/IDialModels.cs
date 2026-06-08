using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

/// <summary>Lists DIAL models via the OpenAI-compatible <c>/openai/models</c> endpoint.</summary>
public interface IDialModels
{
    /// <summary>Gets the models from <c>/openai/models</c>.</summary>
    Task<DialModelList> GetOpenAiAsync(CancellationToken cancellationToken = default);
}
