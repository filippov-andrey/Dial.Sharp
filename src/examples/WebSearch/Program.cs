using Dial.Sharp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Web search: hosted tool that lets the model fetch up-to-date information from the web.
// Dial.Sharp enables it by mapping HostedWebSearchTool to web_search_options on chat completions.
//
// Env: DIAL_ENDPOINT, DIAL_API_KEY, DIAL_DEPLOYMENT (optional)
// Use a deployment that supports web search in your DIAL workspace.

Uri endpoint = new(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")
    ?? throw new InvalidOperationException("Set DIAL_ENDPOINT."));
DialCredential credential = DialCredential.ApiKey(Environment.GetEnvironmentVariable("DIAL_API_KEY")
    ?? throw new InvalidOperationException("Set DIAL_API_KEY."));
string deployment = Environment.GetEnvironmentVariable("DIAL_DEPLOYMENT") ?? "gpt-4o-mini";

using DialClient dial = new(endpoint, credential);
IChatClient chatClient = dial.GetIChatClient(deployment);

ChatClientAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant that can search the web for current information.",
    name: "DialWebSearchAgent",
    tools: [new HostedWebSearchTool()]);

Console.WriteLine(await agent.RunAsync("What major .NET announcements were made at Build 2025?"));
