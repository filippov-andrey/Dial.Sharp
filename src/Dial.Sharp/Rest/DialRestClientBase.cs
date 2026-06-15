using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;

namespace Dial.Sharp.Rest;

internal abstract class DialRestClientBase(ClientPipeline pipeline, Uri endpoint)
{
    protected ClientPipeline Pipeline { get; } = pipeline;
    protected Uri Endpoint { get; } = endpoint;

    protected Uri ResolveUri(string relativePath)
    {
        var baseUri = Endpoint.ToString().TrimEnd('/');
        return new Uri($"{baseUri}/{relativePath.TrimStart('/')}");
    }

    protected Task<T> GetFromJsonAsync<T>(string relativePath, CancellationToken cancellationToken = default) =>
        SendJsonAsync<T>("GET", relativePath, content: null, cancellationToken);

    protected Task<T> PostJsonAsync<T>(string relativePath, object payload, CancellationToken cancellationToken = default) =>
        SendJsonAsync<T>("POST", relativePath, payload, cancellationToken);

    protected async Task PutStreamAsync(
        string relativePath,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        using MemoryStream buffer = new();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        using PipelineMessage message = CreateMessage(cancellationToken);
        message.Request.Method = "PUT";
        message.Request.Uri = ResolveUri(relativePath);
        message.Request.Content = BinaryContent.Create(BinaryData.FromBytes(buffer.ToArray()));
        await Pipeline.SendAsync(message).ConfigureAwait(false);
        EnsureSuccess(message);
    }

    protected async Task<Stream> GetStreamAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        using PipelineMessage message = CreateMessage(cancellationToken);
        message.Request.Method = "GET";
        message.Request.Uri = ResolveUri(relativePath);
        await Pipeline.SendAsync(message).ConfigureAwait(false);
        EnsureSuccess(message);

        var bytes = message.Response!.Content.ToArray();
        return new MemoryStream(bytes, writable: false);
    }

    protected async Task DeleteAtAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        using PipelineMessage message = CreateMessage(cancellationToken);
        message.Request.Method = "DELETE";
        message.Request.Uri = ResolveUri(relativePath);
        await Pipeline.SendAsync(message).ConfigureAwait(false);
        EnsureSuccess(message);
    }

    protected async Task<BinaryData> GetContentAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        using PipelineMessage message = CreateMessage(cancellationToken);
        message.Request.Method = "GET";
        message.Request.Uri = ResolveUri(relativePath);
        message.Request.Headers.Set("Accept", "application/json");
        await Pipeline.SendAsync(message).ConfigureAwait(false);
        EnsureSuccess(message);
        return message.Response!.Content;
    }

    private async Task<T> SendJsonAsync<T>(
        string method,
        string relativePath,
        object? content,
        CancellationToken cancellationToken)
    {
        using PipelineMessage message = CreateMessage(cancellationToken);
        message.Request.Method = method;
        message.Request.Uri = ResolveUri(relativePath);
        message.Request.Headers.Set("Accept", "application/json");

        if (content is not null)
        {
            message.Request.Headers.Set("Content-Type", "application/json");
            message.Request.Content = BinaryContent.Create(
                BinaryData.FromString(JsonSerializer.Serialize(content, DialJsonContext.Default.Options)));
        }

        await Pipeline.SendAsync(message).ConfigureAwait(false);
        return ReadJson<T>(message);
    }

    private static T ReadJson<T>(PipelineMessage message)
    {
        EnsureSuccess(message);
        return JsonSerializer.Deserialize<T>(message.Response!.Content, DialJsonContext.Default.Options)!;
    }

    private PipelineMessage CreateMessage(CancellationToken cancellationToken)
    {
        PipelineMessage message = Pipeline.CreateMessage();
        if (cancellationToken.CanBeCanceled)
        {
            message.Apply(new RequestOptions { CancellationToken = cancellationToken });
        }

        return message;
    }

    private static void EnsureSuccess(PipelineMessage message)
    {
        PipelineResponse? response = message.Response
            ?? throw new InvalidOperationException("DIAL pipeline did not produce a response.");

        if (response.IsError)
        {
            throw new ClientResultException(response);
        }
    }
}
