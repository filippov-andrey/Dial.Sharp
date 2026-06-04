namespace Dial.Sharp;

public static class DialEndpointUriBuilder
{
    public static Uri BuildDeploymentEndpoint(Uri endpoint, string deployment, string apiVersion)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiVersion);

        var baseUri = endpoint.ToString().TrimEnd('/');
        return new Uri($"{baseUri}/openai/deployments/{Uri.EscapeDataString(deployment)}?api-version={Uri.EscapeDataString(apiVersion)}");
    }
}
