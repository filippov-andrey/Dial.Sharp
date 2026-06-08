using System.Diagnostics;
using System.Net;
using System.Text;

namespace Dial.Sharp.Auth;

/// <summary>
/// Default <see cref="IOidcBrowser"/>: launches the system browser and listens on a one-shot loopback HTTP server
/// for the OAuth redirect.
/// </summary>
public sealed class SystemBrowser : IOidcBrowser
{
    private const string ResponseHtml =
        "<html><body>DIAL sign-in complete. You can close this window.</body></html>";

    /// <inheritdoc />
    public async Task<OidcCallbackResult> GetAuthorizationCodeAsync(
        Uri authorizationUrl, Uri redirectUri, string expectedState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authorizationUrl);
        ArgumentNullException.ThrowIfNull(redirectUri);

        using var listener = new HttpListener();
        var prefix = redirectUri.GetLeftPart(UriPartial.Path);
        if (!prefix.EndsWith('/'))
        {
            prefix += "/";
        }

        listener.Prefixes.Add(prefix);
        listener.Start();
        try
        {
            OpenBrowser(authorizationUrl.ToString());

            var context = await listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            var query = ParseQuery(context.Request.Url?.Query);

            await WriteResponseAsync(context.Response, cancellationToken).ConfigureAwait(false);

            if (query.TryGetValue("error", out var error))
            {
                query.TryGetValue("error_description", out var description);
                throw new InvalidOperationException($"OIDC authorization failed: {error} {description}".Trim());
            }

            if (!query.TryGetValue("state", out var state) || !string.Equals(state, expectedState, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("OIDC authorization failed: state mismatch (possible CSRF).");
            }

            if (!query.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
            {
                throw new InvalidOperationException("OIDC authorization failed: no authorization code returned.");
            }

            return new OidcCallbackResult(code, state);
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, CancellationToken cancellationToken)
    {
        var buffer = Encoding.UTF8.GetBytes(ResponseHtml);
        response.ContentType = "text/html";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        response.OutputStream.Close();
    }

    private static Dictionary<string, string> ParseQuery(string? query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(query))
        {
            return result;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
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
