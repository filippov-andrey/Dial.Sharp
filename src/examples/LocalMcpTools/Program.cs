using Dial.Sharp.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

// Local MCP tools: connect to an MCP server (stdio), expose its tools to a Dial-backed agent.
// Chat Completions supports local MCP via function calling; Dial.Sharp carries the tool loop.
//
// Env: DIAL_ENDPOINT, DIAL_BEARER_TOKEN, DIAL_DEPLOYMENT (optional)
// Requires: uvx (uv) on PATH, or `pip install mcp-server-calculator` for python -m fallback.

Uri endpoint = new(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")
                   ?? throw new InvalidOperationException("Set DIAL_ENDPOINT."));
var token = Environment.GetEnvironmentVariable("DIAL_BEARER_TOKEN")
            ?? throw new InvalidOperationException("Set DIAL_BEARER_TOKEN.");
var deployment = Environment.GetEnvironmentVariable("DIAL_DEPLOYMENT") ?? "qwen3.6-27b-awq";

var (command, arguments) = ResolveCalculatorServer();

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

var services = new ServiceCollection();
services.AddDialClient(endpoint, DialBearerToken.From(token));
services.AddDialChatClient(deployment).UseFunctionInvocation();
await using var provider = services.BuildServiceProvider();
var chatClient = provider.GetRequiredService<IChatClient>();

var agent = chatClient.AsAIAgent(
    instructions: "You are a helpful math assistant. Use MCP calculator tools for arithmetic.",
    name: "DialMcpAgent",
    tools: mcpTools);

Console.WriteLine(await agent.RunAsync("What is 15 * 23 + 45?"));
return;

static (string Command, string[] Arguments) ResolveCalculatorServer()
{
    if (TryResolveUvxCommand() is { } uvx)
    {
        return (uvx, ["mcp-server-calculator"]);
    }

    if (TryResolvePythonCommand() is { } python && IsPythonModuleAvailable(python, "mcp_server_calculator"))
    {
        return (python, ["-m", "mcp_server_calculator"]);
    }

    throw new InvalidOperationException(
        "Install uv (https://docs.astral.sh/uv/) — uvx is expected at %USERPROFILE%\\.local\\bin\\uvx.exe on Windows. " +
        "Or: pip install mcp-server-calculator for the python -m fallback.");
}

static string? TryResolveUvxCommand()
{
    if (IsExecutableAvailable("uvx", "--version"))
    {
        return "uvx";
    }

    var localUvx = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local",
        "bin",
        OperatingSystem.IsWindows() ? "uvx.exe" : "uvx");

    return File.Exists(localUvx) && IsExecutableAvailable(localUvx, "--version") ? localUvx : null;
}

static string? TryResolvePythonCommand()
{
    foreach (var candidate in new[] { "python", "python3" })
    {
        if (IsExecutableAvailable(candidate, "--version"))
        {
            return candidate;
        }
    }

    return null;
}

static bool IsPythonModuleAvailable(string python, string module)
{
    try
    {
        using System.Diagnostics.Process process = new();
        process.StartInfo.FileName = python;
        process.StartInfo.Arguments = $"-c \"import importlib.util,sys; sys.exit(0 if importlib.util.find_spec('{module}') else 1)\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        return process.Start() && process.WaitForExit(5_000) && process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}

static bool IsExecutableAvailable(string command, string arguments)
{
    try
    {
        using System.Diagnostics.Process process = new();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        return process.Start() && process.WaitForExit(5_000) && process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}