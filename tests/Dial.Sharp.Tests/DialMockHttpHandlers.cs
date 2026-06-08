namespace Dial.Sharp;

internal sealed class HeaderCapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : DelegatingHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(respond(request));
    }
}

internal sealed class InvocationCountingHandler(Func<int, HttpResponseMessage> respond) : DelegatingHandler
{
    private int _count;

    public int InvocationCount => _count;

    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        var invocation = Interlocked.Increment(ref _count);
        return respond(invocation);
    }
}

internal sealed class ToolCallingHttpHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = await request.Content!.ReadAsStringAsync(cancellationToken);
        var json = body.Contains("\"role\":\"tool\"", StringComparison.Ordinal)
            ? DialTestHost.ChatCompletionJson("It's sunny")
            : """
              {
                "id":"chatcmpl-tools",
                "object":"chat.completion",
                "model":"gpt-4o-mini",
                "choices":[{
                  "index":0,
                  "message":{
                    "role":"assistant",
                    "tool_calls":[{
                      "id":"call_weather",
                      "type":"function",
                      "function":{"name":"GetWeather","arguments":"{}"}
                    }]
                  },
                  "finish_reason":"tool_calls"
                }]
              }
              """;
        return new HttpResponseMessage { Content = new StringContent(json) };
    }
}
