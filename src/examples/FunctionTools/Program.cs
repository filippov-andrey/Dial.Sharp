using System.ComponentModel;
using Dial.Sharp.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

// Function tools: custom C# methods the model can call during a conversation.
// Dial.Sharp maps them to OpenAI function tools on the Chat Completions API.

Uri endpoint = new(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")
    ?? throw new InvalidOperationException("Set DIAL_ENDPOINT."));
var token = Environment.GetEnvironmentVariable("DIAL_BEARER_TOKEN")
            ?? throw new InvalidOperationException("Set DIAL_BEARER_TOKEN.");
var deployment = Environment.GetEnvironmentVariable("DIAL_DEPLOYMENT") ?? "qwen3.6-27b-awq";

var services = new ServiceCollection();
services.AddDialClient(endpoint).WithBearerToken(token);
services.AddDialChatClient(deployment).UseFunctionInvocation();
await using var provider = services.BuildServiceProvider();
var chatClient = provider.GetRequiredService<IChatClient>();

var agent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant. Use tools when needed.",
    name: "DialFunctionToolsAgent",
    tools: [AIFunctionFactory.Create(GetWeather)]);

Console.WriteLine(await agent.RunAsync("Do I need an umbrella in Amsterdam today?"));

[Description("Gets the current weather for a location.")]
static string GetWeather([Description("City name.")] string location) =>
    Random.Shared.NextDouble() > 0.5
        ? $"The weather in {location} is sunny."
        : $"The weather in {location} is raining.";
