using System.ClientModel.Primitives;
using Dial.Sharp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Dial.Sharp;

public class DialDependencyInjectionTests
{
    private static readonly Uri Endpoint = new("https://dial.example.com");

    [Fact]
    public void AddDialClient_WithApiKey_RegistersSingletonDialClient()
    {
        var services = new ServiceCollection();
        services.AddDialClient(Endpoint, "key");
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<DialClient>();
        var second = provider.GetRequiredService<DialClient>();

        Assert.Same(first, second);
        Assert.Equal(Endpoint, first.Endpoint);
    }

    [Fact]
    public void AddDialClient_RegistersAuthenticationPolicy()
    {
        var services = new ServiceCollection();
        services.AddDialClient(Endpoint, "key");
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<AuthenticationPolicy>());
    }

    [Fact]
    public void AddDialChatClient_ResolvesIChatClientAndBuilderChains()
    {
        var services = new ServiceCollection();
        services.AddDialClient(Endpoint, "key");
        services.AddDialChatClient("gpt-4o-mini").UseFunctionInvocation();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IChatClient>());
    }

    [Fact]
    public void AddDialEmbeddingGenerator_ResolvesGenerator()
    {
        var services = new ServiceCollection();
        services.AddDialClient(Endpoint, DialBearerToken.From("token"));
        services.AddDialEmbeddingGenerator("text-embedding-3-small");
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());
    }
}
