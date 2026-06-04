namespace Dial.Sharp;

internal sealed class SseHttpHandler(string expectedInput, params string[] sseDataLines) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Assert.NotNull(request.Content);
        var actualInput = await request.Content.ReadAsStringAsync(cancellationToken);
        VerbatimHttpHandler.AssertContainsNormalized(expectedInput, actualInput);

        var body = string.Join("\n\n", sseDataLines.Select(line => $"data: {line}")) + "\n\ndata: [DONE]\n\n";
        return new HttpResponseMessage
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "text/event-stream"),
        };
    }
}
