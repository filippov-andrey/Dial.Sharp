namespace Dial.Sharp;

public class DialEndpointUriBuilderTests
{
    [Fact]
    public void BuildDeploymentEndpoint_EscapesDeploymentAndVersion()
    {
        Uri uri = DialEndpointUriBuilder.BuildDeploymentEndpoint(
            new Uri("https://dial.example.com/"),
            "my model",
            "2024-10-21");

        Assert.Equal("https://dial.example.com/openai/deployments/my%20model?api-version=2024-10-21", uri.AbsoluteUri);
    }
}

public class DialChatOptionsTests
{
    [Fact]
    public void WithThinking_SetsEnableThinking()
    {
        DialChatOptions options = DialChatOptions.WithThinking();
        Assert.True(options.EnableThinking);
    }

    [Fact]
    public void Clone_CopiesDialProperties()
    {
        DialChatOptions options = new()
        {
            EnableThinking = true,
            Temperature = 0.2f,
        };

        DialChatOptions clone = (DialChatOptions)options.Clone();
        Assert.True(clone.EnableThinking);
        Assert.Equal(0.2f, clone.Temperature);
    }
}

public class DialClientTests
{
    [Fact]
    public void Constructor_ExposesNativeClients()
    {
        using DialClient dial = new(new Uri("https://dial.example.com"), DialCredential.ApiKey("key"));
        Assert.NotNull(dial.Deployments);
        Assert.NotNull(dial.Files);
        Assert.NotNull(dial.Mcp);
        Assert.NotNull(dial.GetIChatClient("gpt-4"));
    }
}
