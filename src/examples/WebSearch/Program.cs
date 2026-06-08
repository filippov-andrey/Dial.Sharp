using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;

// Web search: hosted tool that lets the model fetch up-to-date information from the web.
// Dial.Sharp enables it by mapping HostedWebSearchTool to web_search_options on chat completions.
//
// Env: DIAL_ENDPOINT, DIAL_BEARER_TOKEN, DIAL_DEPLOYMENT (optional)
// Use a deployment that supports web search in your DIAL workspace.

Uri endpoint = new(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")
    ?? throw new InvalidOperationException("Set DIAL_ENDPOINT."));
var credential = new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_BEARER_TOKEN")
                                            ?? throw new InvalidOperationException("Set DIAL_BEARER_TOKEN."));
var deployment = Environment.GetEnvironmentVariable("DIAL_DEPLOYMENT") ?? "qwen3.6-27b-awq";

using DialClient dial = DialClient.WithBearerToken(endpoint, credential);
var chatClient = dial.GetIChatClient(deployment);

var agent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant that can search the web for current information.",
    name: "DialWebSearchAgent",
    tools: [new HostedWebSearchTool()]);

Console.WriteLine(await agent.RunAsync("What major .NET announcements were made at Build 2025?"));
