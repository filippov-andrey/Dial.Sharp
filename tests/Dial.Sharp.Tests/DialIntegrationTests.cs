using System.ClientModel;

namespace Dial.Sharp;

public sealed class DialIntegrationTests
{
    [Fact]
    public async Task LiveChat_SkippedWithoutCredentials()
    {
        var endpoint = Environment.GetEnvironmentVariable("DIAL__Endpoint");
        var apiKey = Environment.GetEnvironmentVariable("DIAL__ApiKey");
        var bearer = Environment.GetEnvironmentVariable("DIAL__BearerToken");
        var deployment = Environment.GetEnvironmentVariable("DIAL__Deployment") ?? "gpt-4";

        if (string.IsNullOrWhiteSpace(endpoint) || (string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(bearer)))
        {
            return;
        }

        using DialClient dial = !string.IsNullOrWhiteSpace(apiKey)
            ? new DialClient(new Uri(endpoint!), new ApiKeyCredential(apiKey))
            : DialClient.WithBearerToken(new Uri(endpoint!), new ApiKeyCredential(bearer!));
        var chat = dial.GetIChatClient(deployment);
        var response = await chat.GetResponseAsync("Say hello in one word.");
        Assert.NotEmpty(response.Messages);
    }
}
