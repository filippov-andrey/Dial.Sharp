using Dial.Sharp.Auth;
using Dial.Sharp.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

// OIDC sign-in: interactive Authorization Code + PKCE via Dial.Sharp.Auth.
// The system browser opens lazily on the first model call; tokens refresh automatically.
//
// Env: DIAL_ENDPOINT, DIAL_DEPLOYMENT (optional), DIAL_OIDC_CLIENT_ID (optional - omit to try Dynamic Client Registration).

Uri endpoint = new(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")
    ?? throw new InvalidOperationException("Set DIAL_ENDPOINT."));
var deployment = Environment.GetEnvironmentVariable("DIAL_DEPLOYMENT") ?? "qwen3.6-27b-awq";

var services = new ServiceCollection();
services.AddDialClient(endpoint, options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("DIAL_OIDC_CLIENT_ID");
});
services.AddDialChatClient(deployment);

await using var provider = services.BuildServiceProvider();
var chatClient = provider.GetRequiredService<IChatClient>();

Console.WriteLine("A browser window will open for sign-in on the first request...");
var response = await chatClient.GetResponseAsync("Say hello in one short sentence.");
Console.WriteLine(response.Text);
