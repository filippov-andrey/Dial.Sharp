using System.ClientModel.Primitives;
using System.Reflection;

namespace Dial.Sharp;

internal static class RequestOptionsExtensions
{
    extension(CancellationToken cancellationToken)
    {
        public RequestOptions ToRequestOptions(bool streaming) =>
            cancellationToken.ToRequestOptions(streaming, policies: null);

        public RequestOptions ToRequestOptions(bool streaming, DialRequestPolicies? policies)
        {
            RequestOptions requestOptions = new()
            {
                CancellationToken = cancellationToken,
                BufferResponse = !streaming,
            };

            requestOptions.AddPolicy(MeaiUserAgentPolicy.Instance, PipelinePosition.PerCall);
            policies?.ApplyTo(requestOptions);
            return requestOptions;
        }
    }

    private sealed class MeaiUserAgentPolicy : PipelinePolicy
    {
        public static MeaiUserAgentPolicy Instance { get; } = new();
        private static readonly string UserAgentValue = CreateUserAgentValue();

        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            AddUserAgentHeader(message);
            ProcessNext(message, pipeline, currentIndex);
        }

        public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline,
            int currentIndex)
        {
            AddUserAgentHeader(message);
            return ProcessNextAsync(message, pipeline, currentIndex);
        }

        private static void AddUserAgentHeader(PipelineMessage message) =>
            message.Request.Headers.Add("User-Agent", UserAgentValue);

        private static string CreateUserAgentValue()
        {
            const string name = "MEAI.Dial";
            if (typeof(MeaiUserAgentPolicy).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion is not { } version) return name;
            var pos = version.IndexOf('+');
            if (pos >= 0)
            {
                version = version[..pos];
            }

            return version.Length > 0 ? $"{name}/{version}" : name;
        }
    }
}