using System.Text.Json;

namespace Dial.Sharp.Rest;

/// <summary>Submits rate (feedback) for a DIAL deployment via <c>/v1/{deployment}/rate</c>.</summary>
public interface IDialRateClient
{
    /// <summary>Posts the rate <paramref name="payload"/> for the deployment.</summary>
    Task<JsonElement> RateAsync(object payload, CancellationToken cancellationToken = default);
}
