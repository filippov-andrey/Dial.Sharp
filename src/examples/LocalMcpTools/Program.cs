using Dial.Sharp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

// Local MCP tools: connect to an MCP server (stdio), expose its tools to a Dial-backed agent.
// Chat Completions supports local MCP via function calling; Dial.Sharp carries the tool loop.
//
// Env: DIAL_ENDPOINT, DIAL_API_KEY, DIAL_DEPLOYMENT (optional)
// Requires: uvx (uv) or npx on PATH for the calculator MCP server.

Uri endpoint = new(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")
    ?? throw new InvalidOperationException("Set DIAL_ENDPOINT."));
DialCredential credential = DialCredential.ApiKey(Environment.GetEnvironmentVariable("DIAL_API_KEY")
    ?? throw new InvalidOperationException("Set DIAL_API_KEY."));
string deployment = Environment.GetEnvironmentVariable("DIAL_DEPLOYMENT") ?? "gpt-4o-mini";

(string command, string[] arguments) = ResolveCalculatorServer();

await using var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "calculator",
    Command = command,
    Arguments = arguments,
}));

IList<AITool> mcpTools = (await mcpClient.ListToolsAsync().ConfigureAwait(false))
    .Cast<AITool>()
    .ToList();

if (mcpTools.Count == 0)
{
    throw new InvalidOperationException("The MCP server did not expose any tools.");
}

using DialClient dial = new(endpoint, credential);
IChatClient chatClient = new ChatClientBuilder(dial.GetIChatClient(deployment))
    .UseFunctionInvocation()
    .Build();

ChatClientAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful math assistant. Use MCP calculator tools for arithmetic.",
    name: "DialMcpAgent",
    tools: mcpTools);

Console.WriteLine(await agent.RunAsync("What is 15 * 23 + 45?"));

static (string Command, string[] Arguments) ResolveCalculatorServer()
{
    if (IsCommandAvailable("uvx"))
    {
        return ("uvx", ["mcp-server-calculator"]);
    }

    if (IsCommandAvailable("npx"))
    {
        return ("npx", ["-y", "mcp-server-calculator"]);
    }

    throw new InvalidOperationException(
        "Install uv (uvx) or Node.js (npx) to run the mcp-server-calculator MCP server.");
}

static bool IsCommandAvailable(string command)
{
    try
    {
        using System.Diagnostics.Process process = new()
        {
            StartInfo =
            {
                FileName = command,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        return process.Start() && process.WaitForExit(5_000) && process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}
