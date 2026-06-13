using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Dial.Sharp.Auth;

/// <summary>
/// Default <see cref="IOidcBrowser"/>: launches the system browser and listens on a one-shot loopback TCP server
/// for the OAuth redirect.
/// </summary>
public sealed class SystemBrowser : IOidcBrowser
{
    private const string ResponseHtml =
        "<html><body>DIAL sign-in complete. You can close this window.</body></html>";

    private readonly TimeSpan _loginTimeout;

    /// <summary>Creates a browser helper with the default five-minute sign-in timeout.</summary>
    public SystemBrowser()
        : this(TimeSpan.FromMinutes(5))
    {
    }

    /// <summary>Creates a browser helper with the supplied sign-in timeout.</summary>
    public SystemBrowser(TimeSpan loginTimeout) => _loginTimeout = loginTimeout;

    /// <inheritdoc />
    public async Task<OidcCallbackResult> GetAuthorizationCodeAsync(
        Uri authorizationUrl, Uri redirectUri, string expectedState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authorizationUrl);
        ArgumentNullException.ThrowIfNull(redirectUri);

        var callbackPath = redirectUri.AbsolutePath;
        if (string.IsNullOrEmpty(callbackPath))
        {
            callbackPath = "/";
        }

        using var loginCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        loginCts.CancelAfter(_loginTimeout);

        var listener = new TcpListener(IPAddress.Loopback, redirectUri.Port);
        listener.Start();
        try
        {
            OpenBrowser(authorizationUrl.ToString());

            while (!loginCts.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(loginCts.Token).ConfigureAwait(false);
                await using var stream = client.GetStream();
                var request = await ReadGetRequestAsync(stream, loginCts.Token).ConfigureAwait(false);
                if (request is null)
                {
                    continue;
                }

                var (method, path, query) = request.Value;
                if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(path, callbackPath, StringComparison.Ordinal))
                {
                    await WriteHttpResponseAsync(stream, "Not found.", 404, loginCts.Token).ConfigureAwait(false);
                    continue;
                }

                var parsedQuery = ParseQuery(query);
                await WriteHttpResponseAsync(stream, ResponseHtml, 200, loginCts.Token).ConfigureAwait(false);

                if (parsedQuery.TryGetValue("error", out var error))
                {
                    parsedQuery.TryGetValue("error_description", out var description);
                    throw new InvalidOperationException($"OIDC authorization failed: {error} {description}".Trim());
                }

                if (!parsedQuery.TryGetValue("state", out var state) ||
                    !string.Equals(state, expectedState, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("OIDC authorization failed: state mismatch (possible CSRF).");
                }

                if (!parsedQuery.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
                {
                    throw new InvalidOperationException("OIDC authorization failed: no authorization code returned.");
                }

                return new OidcCallbackResult(code, state);
            }

            throw new OperationCanceledException(loginCts.Token);
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task<(string Method, string Path, string Query)?> ReadGetRequestAsync(
        Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            stream,
            Encoding.ASCII,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            return null;
        }

        while (true)
        {
            var headerLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(headerLine))
            {
                break;
            }
        }

        return TryParseRequestLine(requestLine, out var method, out var path, out var query)
            ? (method, path, query)
            : null;
    }

    private static bool TryParseRequestLine(string requestLine, out string method, out string path, out string query)
    {
        method = string.Empty;
        path = string.Empty;
        query = string.Empty;

        var parts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        method = parts[0];
        var target = parts[1];
        var queryIndex = target.IndexOf('?');
        if (queryIndex < 0)
        {
            path = target;
        }
        else
        {
            path = target[..queryIndex];
            query = target[(queryIndex + 1)..];
        }

        return true;
    }

    private static async Task WriteHttpResponseAsync(
        Stream stream, string html, int statusCode, CancellationToken cancellationToken)
    {
        var statusText = statusCode switch
        {
            200 => "OK",
            400 => "Bad Request",
            _ => "Not Found",
        };
        var body = Encoding.UTF8.GetBytes(html);
        var headers =
            $"HTTP/1.1 {statusCode} {statusText}\r\n" +
            "Connection: close\r\n" +
            "Content-Type: text/html; charset=utf-8\r\n" +
            $"Content-Length: {body.Length}\r\n\r\n";
        var response = Encoding.UTF8.GetBytes(headers).Concat(body).ToArray();
        await stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(query))
        {
            return result;
        }

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var index = pair.IndexOf('=');
            var key = Uri.UnescapeDataString(index < 0 ? pair : pair[..index]);
            var value = index < 0 ? string.Empty : Uri.UnescapeDataString(pair[(index + 1)..]);
            result[key] = value;
        }

        return result;
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{url}\"") { CreateNoWindow = true });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
            }
            else
            {
                Process.Start("xdg-open", url);
            }
        }
    }
}
