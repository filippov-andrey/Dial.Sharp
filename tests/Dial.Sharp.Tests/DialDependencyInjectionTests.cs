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
        services.AddDialClient(Endpoint).WithApiKey("key");
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<DialClient>();
        var second = provider.GetRequiredService<DialClient>();

        Assert.Same(first, second);
        Assert.Equal(Endpoint, first.Endpoint);
    }

    [Fact]
    public void AddDialClient_RegistersNamedHttpClient()
    {
        var services = new ServiceCollection();
        services.AddDialClient(Endpoint).WithApiKey("key");
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using var http = factory.CreateClient(DialServiceCollectionExtensions.HttpClientName);

        Assert.NotNull(http);
    }

    [Fact]
    public void AddDialChatClient_ResolvesIChatClientAndBuilderChains()
    {
        var services = new ServiceCollection();
        services.AddDialClient(Endpoint).WithApiKey("key");
        services.AddDialChatClient("gpt-4o-mini").UseFunctionInvocation();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IChatClient>());
    }

    [Fact]
    public void AddDialEmbeddingGenerator_ResolvesGenerator()
    {
        var services = new ServiceCollection();
        services.AddDialClient(Endpoint).WithBearerToken("token");
        services.AddDialEmbeddingGenerator("text-embedding-3-small");
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());
    }

    [Fact]
    public void AddDialClient_WithoutConfiguredAuth_ThrowsOnResolve()
    {
        var services = new ServiceCollection();
        services.AddDialClient(Endpoint);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<DialClient>());
    }
}
