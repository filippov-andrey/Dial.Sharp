using System.ClientModel;
using System.ClientModel.Primitives;

namespace Dial.Sharp.Auth;

/// <summary>Factory methods for DIAL <see cref="AuthenticationPolicy"/> instances.</summary>
internal static class DialAuthenticationPolicies
{
    internal static AuthenticationPolicy ForApiKey(ApiKeyCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        return ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(credential, "Api-Key");
    }

    internal static AuthenticationPolicy ForBearer(ApiKeyCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        return ApiKeyAuthenticationPolicy.CreateBearerAuthorizationPolicy(credential);
    }

    internal static AuthenticationPolicy ForOidc(AuthenticationTokenProvider tokenProvider)
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);
        return new BearerTokenPolicy(tokenProvider, "openid");
    }

    internal static AuthenticationPolicy Passthrough { get; } = new PassthroughAuthenticationPolicy();

    private sealed class PassthroughAuthenticationPolicy : AuthenticationPolicy
    {
        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex) =>
            ProcessNext(message, pipeline, currentIndex);

        public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            ProcessNext(message, pipeline, currentIndex);
            return default;
        }
    }
}
