using System.ComponentModel;
using Dial.Sharp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Function tools: custom C# methods the model can call during a conversation.
// Dial.Sharp maps them to OpenAI function tools on the Chat Completions API.
//
// Env: DIAL_ENDPOINT, DIAL_API_KEY, DIAL_DEPLOYMENT (optional, default gpt-4o-mini)

Uri endpoint = new(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")
    ?? throw new InvalidOperationException("Set DIAL_ENDPOINT."));
DialCredential credential = DialCredential.ApiKey(Environment.GetEnvironmentVariable("DIAL_API_KEY")
    ?? throw new InvalidOperationException("Set DIAL_API_KEY."));
string deployment = Environment.GetEnvironmentVariable("DIAL_DEPLOYMENT") ?? "gpt-4o-mini";

using DialClient dial = new(endpoint, credential);
IChatClient chatClient = new ChatClientBuilder(dial.GetIChatClient(deployment))
    .UseFunctionInvocation()
    .Build();

ChatClientAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant. Use tools when needed.",
    name: "DialFunctionToolsAgent",
    tools: [AIFunctionFactory.Create(GetWeather)]);

Console.WriteLine(await agent.RunAsync("Do I need an umbrella in Amsterdam today?"));

[Description("Gets the current weather for a location.")]
static string GetWeather([Description("City name.")] string location) =>
    Random.Shared.NextDouble() > 0.5
        ? $"The weather in {location} is sunny."
        : $"The weather in {location} is raining.";
