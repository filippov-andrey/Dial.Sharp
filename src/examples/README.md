# Dial.Sharp — Agent Framework tool examples

Dial.Sharp exposes DIAL deployments through `IChatClient` (OpenAI **Chat Completions** API). The same client works with [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/overview/agent-framework-overview) (`ChatClientAgent`).

This folder contains runnable samples for each [Agent Framework tool type](https://learn.microsoft.com/en-us/agent-framework/agents/tools/?pivots=programming-language-csharp) that Dial.Sharp supports today via Chat Completions:

| Tool type | Example | Dial.Sharp support |
| --- | --- | --- |
| Function Tools | [FunctionTools](FunctionTools/) | `AIFunction` / `AIFunctionFactory` in `ChatOptions.Tools` or `AsAIAgent(..., tools: ...)` |
| Web Search | [WebSearch](WebSearch/) | `HostedWebSearchTool` → DIAL `web_search_options` |
| Local MCP Tools | [LocalMcpTools](LocalMcpTools/) | MCP C# SDK tools passed to `ChatClientAgent` (function-calling over DIAL) |
| Speech-to-Text | [SpeechToText](SpeechToText/) | `ISpeechToTextClient` → DIAL ASR via `custom_content.attachments` |

Not included (Chat Completions / Dial.Sharp do not wire these hosted tools yet):

| Tool type | Why omitted |
| --- | --- |
| Code Interpreter | Responses API hosted tool only; use DIAL REST `DialClient.CodeInterpreter` separately if needed |
| File Search | Responses API hosted tool only |
| Hosted MCP Tools | Responses API hosted tool only |

## Prerequisites

Set `DIAL_ENDPOINT`, `DIAL_BEARER_TOKEN`, and optionally `DIAL_DEPLOYMENT` (default `qwen3.6-27b-awq`).

For **Web Search**, use a DIAL deployment that supports web search for your provider.

For **Local MCP Tools**, install [uv](https://docs.astral.sh/uv/) (`uvx mcp-server-calculator`). Fallback: `pip install mcp-server-calculator` and `python -m mcp_server_calculator`.

For **Speech-to-Text**, use a DIAL ASR deployment (e.g. `qwen3-asr`). Run without arguments for real-time dictation: each pause in speech is sent to the model (Windows, macOS, Linux via PortAudio). Or pass a local audio file path (or set `DIAL_AUDIO_FILE`).

## Run

Create `Properties/launchSettings.json` locally (gitignored) or set the env vars above before running from the IDE.

From the repository root:

```console
dotnet run --project src/examples/FunctionTools
dotnet run --project src/examples/WebSearch
dotnet run --project src/examples/LocalMcpTools
dotnet run --project src/examples/SpeechToText
dotnet run --project src/examples/SpeechToText -- path/to/audio.wav
```
