# Dial.Sharp

[![CI](https://github.com/filippov-andrey/Dial.Sharp/actions/workflows/ci.yml/badge.svg)](https://github.com/filippov-andrey/Dial.Sharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/AI.Dial.Sharp?label=NuGet)](https://www.nuget.org/packages/AI.Dial.Sharp)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**[DIAL](https://dialx.ai/)** (Deterministic Integrator of Applications and Language Models) is EPAM’s open-source AI orchestration platform for building and operating enterprise GenAI applications. It provides a unified, model-agnostic [Core API](https://dialx.ai/dial_api) (OpenAI-compatible for chat and embeddings) plus services for applications, files, tools, access control, and rate limits — so teams can use many LLMs and DIAL apps through one integration layer instead of separate provider SDKs.

**Dial.Sharp** is a .NET package that connects that API to [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai): `IChatClient`, `IEmbeddingGenerator`, and `ISpeechToTextClient` for OpenAI-compatible deployments, plus `DialClient` for DIAL-native REST endpoints (catalog, tokenize, MCP, and more). The same `IChatClient` works with [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/overview/agent-framework-overview) (`ChatClientAgent`).

## Install the package

NuGet package ID: **`AI.Dial.Sharp`**. C# namespace and assembly: **`Dial.Sharp`**.

From the command-line:

```console
dotnet add package AI.Dial.Sharp
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="AI.Dial.Sharp" Version="[CURRENTVERSION]" />
</ItemGroup>
```

`ChatClientBuilder`, hosting extensions, caching, and OpenTelemetry middleware require the [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI) package (and related hosting/cache/telemetry packages as needed). For agents, add [Microsoft.Agents.AI](https://www.nuget.org/packages/Microsoft.Agents.AI) separately (not a dependency of `AI.Dial.Sharp`).

## Usage Examples

Set `DIAL_ENDPOINT` and `DIAL_API_KEY` (or use `DialClient.WithBearerToken` for OIDC). The deployment name is the model id configured in DIAL (examples use `gpt-4o-mini`).

### Chat

```csharp
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;

IChatClient client =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIChatClient("gpt-4o-mini");

Console.WriteLine(await client.GetResponseAsync("What is AI?"));
```

### Chat + Conversation History

```csharp
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;

IChatClient client =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIChatClient("gpt-4o-mini");

Console.WriteLine(await client.GetResponseAsync(
[
    new ChatMessage(ChatRole.System, "You are a helpful AI assistant"),
    new ChatMessage(ChatRole.User, "What is AI?"),
]));
```

### Chat streaming

```csharp
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;

IChatClient client =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIChatClient("gpt-4o-mini");

await foreach (var update in client.GetStreamingResponseAsync("What is AI?"))
{
    Console.Write(update);
}
```

### Microsoft Agent Framework

Wrap the DIAL-backed `IChatClient` with [`AsAIAgent`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.chatclientextensions.asaiagent):

```csharp
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

IChatClient chatClient =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIChatClient("gpt-4o-mini");

ChatClientAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant.",
    name: "DialAgent");

AgentResponse response = await agent.RunAsync("What is AI?");
Console.WriteLine(response.Text);
```

```console
dotnet add package Microsoft.Agents.AI
```

### Agent Framework provider support matrix

Dial.Sharp exposes DIAL deployments through `IChatClient` on the OpenAI **Chat Completions** API. The matrix below follows the [Agent Framework tools overview](https://learn.microsoft.com/en-us/agent-framework/agents/tools/?pivots=programming-language-csharp) and shows which [tool types](https://learn.microsoft.com/en-us/agent-framework/agents/tools/?pivots=programming-language-csharp#tool-types) work with `ChatClientAgent` backed by `DialClient`.

| Tool Type | Dial (Chat Completions) |
| --- | --- |
| Function Tools | ✅ |
| Code Interpreter | ❌ |
| File Search | ❌ |
| Web Search | ✅ |
| Hosted MCP Tools | ❌ |
| Local MCP Tools | ✅ |

This matches the **Chat Completion** column in the [Agent Framework provider support matrix](https://learn.microsoft.com/en-us/agent-framework/agents/tools/?pivots=programming-language-csharp#provider-support-matrix): hosted tools such as code interpreter, file search, and hosted MCP require the OpenAI Responses API, which Dial.Sharp does not use today.

**Notes**

- **Function tools** and **local MCP tools** need `ChatClientBuilder.UseFunctionInvocation()` (or equivalent agent middleware) so tool calls are executed and results are sent back to the model.
- **Web search** requires `HostedWebSearchTool` in `ChatOptions.Tools` / `AsAIAgent(..., tools: ...)` and a DIAL deployment whose upstream model supports web search.
- **Tool approval** is an Agent Framework feature (not provider-specific) and works with Dial when function invocation runs locally via `Microsoft.Extensions.AI`.
- DIAL also exposes separate REST endpoints — `DialClient.CodeInterpreter`, `DialClient.Mcp`, `DialClient.Files` — that are outside this Agent Framework tool matrix. See [DIAL Core API](https://dialx.ai/dial_api).

Runnable samples for supported tool types and speech-to-text: [`src/examples/`](src/examples/) (including [SpeechToText](src/examples/SpeechToText/) for real-time microphone dictation).

### DIAL thinking models

```csharp
using System.ClientModel;
using Dial.Sharp;
using Dial.Sharp.Inference;
using Microsoft.Extensions.AI;

IChatClient client =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIChatClient("gpt-4o-mini");

DialChatOptions options = new()
{
    EnableThinking = true,
    Reasoning = new ReasoningOptions
    {
        Effort = ReasoningEffort.Medium,
        Output = ReasoningOutput.Full,
    },
};

Console.WriteLine(await client.GetResponseAsync("Explain recursion briefly.", options));
```

### Arbitrary DIAL request fields

For DIAL fields that have no typed option, set them directly on the underlying `ChatCompletionOptions.Patch` via `RawRepresentationFactory`. Use the raw-JSON `Patch.Set(path, ReadOnlySpan<byte>)` overload for object/array values (the `string` overload writes a JSON string):

```csharp
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

IChatClient client =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIChatClient("gpt-4o-mini");

ChatOptions options = new()
{
    RawRepresentationFactory = _ =>
    {
        ChatCompletionOptions raw = new();
        raw.Patch.Set("$.chat_template_kwargs.enable_thinking"u8, true);
        raw.Patch.Set("$.custom_fields"u8, """{"key":"value"}"""u8); // raw-JSON overload
        return raw;
    },
};

Console.WriteLine(await client.GetResponseAsync("What is AI?", options));
```

### Tool calling

```csharp
using System.ComponentModel;
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;

IChatClient dialClient =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder(dialClient)
    .UseFunctionInvocation()
    .Build();

ChatOptions chatOptions = new()
{
    Tools = [AIFunctionFactory.Create(GetWeather)],
};

await foreach (var message in client.GetStreamingResponseAsync("Do I need an umbrella?", chatOptions))
{
    Console.Write(message);
}

[Description("Gets the weather")]
static string GetWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
```

### Caching

```csharp
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

IChatClient dialClient =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder(dialClient)
    .UseDistributedCache(cache)
    .Build();

for (int i = 0; i < 3; i++)
{
    await foreach (var message in client.GetStreamingResponseAsync("In less than 100 words, what is AI?"))
    {
        Console.Write(message);
    }

    Console.WriteLine();
    Console.WriteLine();
}
```

### Telemetry

```csharp
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;
using OpenTelemetry.Trace;

var sourceName = Guid.NewGuid().ToString();
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

IChatClient dialClient =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder(dialClient)
    .UseOpenTelemetry(sourceName: sourceName, configure: c => c.EnableSensitiveData = true)
    .Build();

Console.WriteLine(await client.GetResponseAsync("What is AI?"));
```

### Telemetry, Caching, and Tool Calling

```csharp
using System.ComponentModel;
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

var sourceName = Guid.NewGuid().ToString();
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

var chatOptions = new ChatOptions
{
    Tools = [AIFunctionFactory.Create(GetPersonAge)],
};

IChatClient dialClient =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder(dialClient)
    .UseDistributedCache(cache)
    .UseFunctionInvocation()
    .UseOpenTelemetry(sourceName: sourceName, configure: c => c.EnableSensitiveData = true)
    .Build();

for (int i = 0; i < 3; i++)
{
    Console.WriteLine(await client.GetResponseAsync("How much older is Alice than Bob?", chatOptions));
}

[Description("Gets the age of a person specified by name.")]
static int GetPersonAge(string personName) =>
    personName switch
    {
        "Alice" => 42,
        "Bob" => 35,
        _ => 26,
    };
```

### Text embedding generation

```csharp
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;

IEmbeddingGenerator<string, Embedding<float>> generator =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIEmbeddingGenerator("text-embedding-3-small");

var embeddings = await generator.GenerateAsync("What is AI?");

Console.WriteLine(string.Join(", ", embeddings[0].Vector.ToArray()));
```

### Text embedding generation with caching

```csharp
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

IEmbeddingGenerator<string, Embedding<float>> dialGenerator =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetIEmbeddingGenerator("text-embedding-3-small");

IEmbeddingGenerator<string, Embedding<float>> generator =
    new EmbeddingGeneratorBuilder<string, Embedding<float>>(dialGenerator)
        .UseDistributedCache(cache)
        .Build();

foreach (var prompt in new[] { "What is AI?", "What is .NET?", "What is AI?" })
{
    var embeddings = await generator.GenerateAsync(prompt);
    Console.WriteLine(string.Join(", ", embeddings[0].Vector.ToArray()));
}
```

### Dependency Injection

`AddDialClient` registers a singleton `DialClient` and requires authentication up front:

- **Api-Key:** `AddDialClient(endpoint, apiKey)`
- **Bearer:** `AddDialClient(endpoint, DialBearerToken.From(token))`
- **OIDC** (`AI.Dial.Sharp.Auth`): `AddDialClient(endpoint, oidcOptions => { ... })`

`AddDialChatClient` and `AddDialEmbeddingGenerator` return the Microsoft.Extensions.AI `ChatClientBuilder` / `EmbeddingGeneratorBuilder`, so you can chain middleware:

```csharp
using Dial.Sharp;
using Dial.Sharp.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddDistributedMemoryCache();

builder.Services
    .AddDialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        Environment.GetEnvironmentVariable("DIAL_API_KEY")!);

builder.Services.AddDialChatClient("gpt-4o-mini")
    .UseDistributedCache()
    .UseFunctionInvocation();

builder.Services.AddDialEmbeddingGenerator("text-embedding-3-small");

var app = builder.Build();

var chatClient = app.Services.GetRequiredService<IChatClient>();
Console.WriteLine(await chatClient.GetResponseAsync("What is AI?"));
```

### OIDC sign-in (Dial.Sharp.Auth)

The optional **`AI.Dial.Sharp.Auth`** package adds interactive OIDC sign-in (Authorization Code + PKCE, automatic refresh, and optional Dynamic Client Registration). Pass an OIDC configuration delegate to `AddDialClient` — the system browser opens lazily on the first request:

```console
dotnet add package AI.Dial.Sharp.Auth
```

```csharp
using Dial.Sharp;
using Dial.Sharp.Auth;
using Dial.Sharp.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

Uri endpoint = new(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!);

var services = new ServiceCollection();
services.AddDialClient(endpoint, options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("DIAL_OIDC_CLIENT_ID"); // omit to try Dynamic Client Registration
});
services.AddDialChatClient("gpt-4o-mini");

using var provider = services.BuildServiceProvider();
var chatClient = provider.GetRequiredService<IChatClient>();
Console.WriteLine(await chatClient.GetResponseAsync("What is AI?"));
```

Tokens are kept by `IDialTokenStore` (default: in-memory). Register your own implementation to persist them (for example, in an OS keychain). Without DI, call `DialOidcSession.Create(options).CreateDialClient()` to get a `DialClient` whose requests carry the refreshing token. Run the [Oidc example](src/examples/Oidc/):

```console
dotnet run --project src/examples/Oidc
```

### Minimal Web API

```csharp
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

Uri endpoint = new(builder.Configuration["DIAL:Endpoint"]!);
ApiKeyCredential credential = new(builder.Configuration["DIAL:ApiKey"]!);

builder.Services.AddSingleton(_ => new DialClient(endpoint, credential));
builder.Services.AddChatClient(services =>
    services.GetRequiredService<DialClient>().GetIChatClient(builder.Configuration["DIAL:ChatDeployment"]!));
builder.Services.AddEmbeddingGenerator(services =>
    services.GetRequiredService<DialClient>().GetIEmbeddingGenerator(builder.Configuration["DIAL:EmbeddingDeployment"]!));

var app = builder.Build();

app.MapPost("/chat", async (IChatClient client, string message) =>
{
    var response = await client.GetResponseAsync(message);
    return response.Text;
});

app.MapPost("/embedding", async (IEmbeddingGenerator<string, Embedding<float>> client, string message) =>
{
    var response = await client.GenerateAsync(message);
    return response[0].Vector;
});

app.Run();
```

### Speech-to-text (DIAL ASR)

DIAL ASR deployments (for example `qwen3-asr`) transcribe audio through **chat completions** with `custom_content.attachments`, not the OpenAI `/audio/transcriptions` endpoint. Dial.Sharp maps them to [`ISpeechToTextClient`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.ispeechtotextclient):

```csharp
using System.ClientModel;
using Dial.Sharp;
using Microsoft.Extensions.AI;
using OpenAI.Audio;

ISpeechToTextClient speechToText =
    new DialClient(
        new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!))
    .GetISpeechToTextClient("qwen3-asr");

await using var audio = File.OpenRead("sample.wav");
var response = await speechToText.GetTextAsync(audio, new SpeechToTextOptions
{
    RawRepresentationFactory = _ => new AudioTranscriptionOptions
    {
        Prompt = "Transcribe this audio.",
    },
});

Console.WriteLine(response.Text);
```

For real-time dictation from the microphone (cross-platform via PortAudio), run the [SpeechToText example](src/examples/SpeechToText/):

```console
dotnet run --project src/examples/SpeechToText
```

### DIAL REST APIs

`DialClient` also exposes deployment catalog, files, tokenize, MCP, and related DIAL endpoints:

```csharp
using System.ClientModel;
using Dial.Sharp;
using Dial.Sharp.Tokenization;

using DialClient dial = new(
    new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")!),
    new ApiKeyCredential(Environment.GetEnvironmentVariable("DIAL_API_KEY")!));

var catalog = await dial.DeploymentCatalog.GetAsync();
var counter = dial.GetTokenCounter("gpt-4o-mini");
int tokens = await counter.CountStringAsync("hello");
```

## Documentation

- [DIAL Core API](https://dialx.ai/dial_api)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
- [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/overview/agent-framework-overview)
- [Quickstart - Build an AI chat app with .NET](https://learn.microsoft.com/dotnet/ai/quickstarts/build-chat-app) — follow the guide and replace the sample provider with `DialClient` as shown above

## Building this repository

```bash
dotnet build Dial.Sharp.slnx
dotnet test Dial.Sharp.slnx
```
