# Dial.Sharp — Agent Framework tool examples

Dial.Sharp exposes DIAL deployments through `IChatClient` (OpenAI **Chat Completions** API). The same client works with [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/overview/agent-framework-overview) (`ChatClientAgent`).

This folder contains runnable samples for each [Agent Framework tool type](https://learn.microsoft.com/en-us/agent-framework/agents/tools/?pivots=programming-language-csharp) that Dial.Sharp supports today via Chat Completions:

| Tool type | Example | Dial.Sharp support |
| --- | --- | --- |
| Function Tools | [FunctionTools](FunctionTools/) | `AIFunction` / `AIFunctionFactory` in `ChatOptions.Tools` or `AsAIAgent(..., tools: ...)` |
| Web Search | [WebSearch](WebSearch/) | `HostedWebSearchTool` → DIAL `web_search_options` |
| Local MCP Tools | [LocalMcpTools](LocalMcpTools/) | MCP C# SDK tools passed to `ChatClientAgent` (function-calling over DIAL) |

Not included (Chat Completions / Dial.Sharp do not wire these hosted tools yet):

| Tool type | Why omitted |
| --- | --- |
| Code Interpreter | Responses API hosted tool only; use DIAL REST `DialClient.CodeInterpreter` separately if needed |
| File Search | Responses API hosted tool only |
| Hosted MCP Tools | Responses API hosted tool only |

## Prerequisites

Set environment variables (or copy from your DIAL workspace):

```console
set DIAL_ENDPOINT=https://your-dial-host/
set DIAL_API_KEY=your-api-key
set DIAL_DEPLOYMENT=gpt-4o-mini
```

For **Web Search**, use a DIAL deployment that supports web search for your provider.

For **Local MCP Tools**, install a calculator MCP server runtime (one of):

- [uv](https://docs.astral.sh/uv/) — `uvx mcp-server-calculator`
- Node.js — `npx -y mcp-server-calculator`

## Run

From the repository root:

```console
dotnet run --project src/examples/FunctionTools
dotnet run --project src/examples/WebSearch
dotnet run --project src/examples/LocalMcpTools
```
