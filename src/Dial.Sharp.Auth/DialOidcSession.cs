using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dial.Sharp.Auth.Internal;

namespace Dial.Sharp.Auth;

/// <summary>
/// Holds an interactive OIDC session for DIAL: signs in lazily on first use (Authorization Code + PKCE, with optional
/// Dynamic Client Registration) and returns a valid access token, refreshing before expiry.
/// </summary>
public sealed class DialOidcSession : IDisposable
{
    private readonly DialOidcOptions _options;
    private readonly IDialTokenStore _store;
    private readonly HttpClient _idpClient;
    private readonly bool _ownsIdpClient;
    private readonly IOidcBrowser _browser;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<IDisposable> _owned = [];

    private OidcDiscoveryDocument? _discovery;
    private string? _clientId;
    private string? _clientSecret;
    private DialTokenSet? _tokens;
    private bool _tokensLoaded;

    internal DialOidcSession(
        DialOidcOptions options,
        IDialTokenStore store,
        HttpClient idpClient,
        bool ownsIdpClient,
        IOidcBrowser browser,
        TimeProvider timeProvider)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.ServerUrl is null)
        {
            throw new ArgumentException("DialOidcOptions.ServerUrl must be set.", nameof(options));
        }

        _store = store ?? throw new ArgumentNullException(nameof(store));
        _idpClient = idpClient ?? throw new ArgumentNullException(nameof(idpClient));
        _ownsIdpClient = ownsIdpClient;
        _browser = browser ?? throw new ArgumentNullException(nameof(browser));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _clientId = options.ClientId;
        _clientSecret = options.ClientSecret;
    }

    /// <summary>The configured DIAL server URL.</summary>
    public Uri ServerUrl => _options.ServerUrl!;

    /// <summary>Creates a session for non-DI usage with sensible defaults.</summary>
    public static DialOidcSession Create(
        DialOidcOptions options,
        IDialTokenStore? store = null,
        HttpClient? idpHttpClient = null,
        IOidcBrowser? browser = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var ownsClient = idpHttpClient is null;
        return new DialOidcSession(
            options,
            store ?? new InMemoryDialTokenStore(),
            idpHttpClient ?? new HttpClient(),
            ownsClient,
            browser ?? new SystemBrowser(),
            TimeProvider.System);
    }

    /// <summary>Returns a valid access token, signing in or refreshing as needed.</summary>
    public async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_tokensLoaded)
            {
                _tokens = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
                ApplyClientIdFromTokens(_tokens);
                _tokensLoaded = true;
            }

            if (_tokens is null)
            {
                await SetTokensAsync(await LoginAsync(cancellationToken).ConfigureAwait(false), cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (IsExpired(_tokens))
            {
                var refreshed = await RefreshAsync(_tokens, cancellationToken).ConfigureAwait(false)
                                ?? await LoginAsync(cancellationToken).ConfigureAwait(false);
                await SetTokensAsync(refreshed, cancellationToken).ConfigureAwait(false);
            }

            return _tokens!.AccessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Clears the stored tokens, forcing a fresh sign-in on the next call.</summary>
    public async ValueTask SignOutAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _tokens = null;
            await _store.ClearAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _gate.Dispose();
        if (_ownsIdpClient)
        {
            _idpClient.Dispose();
        }

        foreach (var disposable in _owned)
        {
            disposable.Dispose();
        }
    }

    internal void Track(IDisposable disposable) => _owned.Add(disposable);

    private bool IsExpired(DialTokenSet tokens) =>
        _timeProvider.GetUtcNow() >= tokens.ExpiresAtUtc - _options.RefreshSkew;

    private async ValueTask SetTokensAsync(DialTokenSet tokens, CancellationToken cancellationToken)
    {
        _tokens = tokens with { ClientId = tokens.ClientId ?? _clientId };
        ApplyClientIdFromTokens(_tokens);
        await _store.SaveAsync(_tokens, cancellationToken).ConfigureAwait(false);
    }

    private void ApplyClientIdFromTokens(DialTokenSet? tokens)
    {
        if (!string.IsNullOrEmpty(tokens?.ClientId))
        {
            _clientId ??= tokens.ClientId;
        }
    }

    private async Task<DialTokenSet> LoginAsync(CancellationToken cancellationToken)
    {
        var discovery = await EnsureDiscoveryAsync(cancellationToken).ConfigureAwait(false);
        await EnsureClientIdAsync(discovery, cancellationToken).ConfigureAwait(false);

        var (verifier, challenge) = Pkce.Create();
        var state = Pkce.CreateState();
        var redirectUri = BuildRedirectUri();

        var authorizationUrl = BuildAuthorizationUrl(discovery.AuthorizationEndpoint!, challenge, state, redirectUri);
        // Do not tie the loopback callback wait to the caller's HTTP cancellation token: SDK retries/timeouts
        // would stop listening while the user is still signing in through the browser.
        var callback = await _browser
            .GetAuthorizationCodeAsync(authorizationUrl, redirectUri, state, CancellationToken.None)
            .ConfigureAwait(false);

        return await ExchangeCodeAsync(discovery, callback.Code, verifier, redirectUri, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<OidcDiscoveryDocument> EnsureDiscoveryAsync(CancellationToken cancellationToken)
    {
        if (_discovery is not null)
        {
            return _discovery;
        }

        var url = ServerUrl.ToString().TrimEnd('/') + "/.well-known/openid-configuration";
        var document = await _idpClient
            .GetFromJsonAsync(url, DialAuthJsonContext.Default.OidcDiscoveryDocument, cancellationToken)
            .ConfigureAwait(false);

        if (document?.AuthorizationEndpoint is null || document.TokenEndpoint is null)
        {
            throw new InvalidOperationException("OIDC discovery failed: missing authorization or token endpoint.");
        }

        return _discovery = document;
    }

    private async Task EnsureClientIdAsync(OidcDiscoveryDocument discovery, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_clientId))
        {
            return;
        }

        if (string.IsNullOrEmpty(discovery.RegistrationEndpoint))
        {
            throw new InvalidOperationException(
                "No ClientId configured and the IdP does not advertise a registration_endpoint for Dynamic Client Registration.");
        }

        var redirectUri = BuildRedirectUri().ToString();
        Exception? lastError = null;
        var failures = new List<string>();

        foreach (var (url, body) in KeycloakClientRegistration.BuildAttempts(
                     discovery.RegistrationEndpoint, redirectUri, _options))
        {
            try
            {
                var registered = await PostRegistrationAsync(url, body, cancellationToken).ConfigureAwait(false);
                _clientId = registered.ClientId;
                _clientSecret ??= registered.ClientSecret;
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                failures.Add(ex.Message);
            }
        }

        var detail = failures.Count > 0
            ? Environment.NewLine + string.Join(Environment.NewLine, failures)
            : string.Empty;
        throw new InvalidOperationException(
            "Dynamic Client Registration failed for all known registration endpoints." + detail,
            lastError);
    }

    private async Task<DcrResponse> PostRegistrationAsync(Uri url, object body, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = body switch
            {
                DcrRequest request => JsonContent.Create(request, DialAuthJsonContext.Default.DcrRequest),
                KeycloakDefaultDcrRequest keycloak => JsonContent.Create(
                    keycloak, DialAuthJsonContext.Default.KeycloakDefaultDcrRequest),
                _ => throw new InvalidOperationException($"Unsupported registration payload: {body.GetType().Name}"),
            },
        };
        if (!string.IsNullOrEmpty(_options.InitialAccessToken))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.InitialAccessToken);
        }

        using var response = await _idpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Dynamic Client Registration failed for {url}: {(int)response.StatusCode} {response.ReasonPhrase}. {errorBody}".Trim());
        }

        var registered = await response.Content
            .ReadFromJsonAsync(DialAuthJsonContext.Default.DcrResponse, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(registered?.ResolvedClientId))
        {
            throw new InvalidOperationException("Dynamic Client Registration did not return a client_id.");
        }

        registered.ClientId = registered.ResolvedClientId;
        registered.ClientSecret ??= registered.ResolvedClientSecret;
        return registered;
    }

    private async Task<DialTokenSet> ExchangeCodeAsync(
        OidcDiscoveryDocument discovery, string code, string verifier, Uri redirectUri, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri.ToString(),
            ["client_id"] = _clientId!,
            ["code_verifier"] = verifier,
        };
        if (!string.IsNullOrEmpty(_clientSecret))
        {
            fields["client_secret"] = _clientSecret;
        }

        var token = await PostTokenAsync(discovery.TokenEndpoint!, fields, cancellationToken).ConfigureAwait(false);
        return token is null || token.AccessToken is null
            ? throw new InvalidOperationException($"OIDC token exchange failed: {token?.Error} {token?.ErrorDescription}".Trim())
            : ToTokenSet(token);
    }

    private async Task<DialTokenSet?> RefreshAsync(DialTokenSet current, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(current.RefreshToken))
        {
            return null;
        }

        var discovery = await EnsureDiscoveryAsync(cancellationToken).ConfigureAwait(false);
        var clientId = ResolveClientId(current);
        var fields = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = current.RefreshToken,
            ["client_id"] = clientId,
        };
        if (!string.IsNullOrEmpty(_clientSecret))
        {
            fields["client_secret"] = _clientSecret;
        }

        OidcTokenResponse? token;
        try
        {
            token = await PostTokenAsync(discovery.TokenEndpoint!, fields, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        if (token?.AccessToken is null)
        {
            return null;
        }

        // Honor refresh-token rotation: fall back to the previous token if the IdP did not issue a new one.
        return ToTokenSet(token, fallbackRefreshToken: current.RefreshToken, fallbackClientId: current.ClientId);
    }

    private string ResolveClientId(DialTokenSet? tokens = null) =>
        _clientId ?? tokens?.ClientId ?? _options.ClientId
        ?? throw new InvalidOperationException("OAuth client_id is not available.");

    private async Task<OidcTokenResponse?> PostTokenAsync(
        string tokenEndpoint, IDictionary<string, string> fields, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(fields);
        using var response = await _idpClient.PostAsync(tokenEndpoint, content, cancellationToken).ConfigureAwait(false);
        var token = await response.Content
            .ReadFromJsonAsync(DialAuthJsonContext.Default.OidcTokenResponse, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode && token?.AccessToken is null)
        {
            throw new HttpRequestException(
                $"Token endpoint returned {(int)response.StatusCode}: {token?.Error} {token?.ErrorDescription}".Trim());
        }

        return token;
    }

    private DialTokenSet ToTokenSet(
        OidcTokenResponse token,
        string? fallbackRefreshToken = null,
        string? fallbackClientId = null)
    {
        var lifetime = TimeSpan.FromSeconds(token.ExpiresIn ?? 300);
        return new DialTokenSet(
            token.AccessToken!,
            string.IsNullOrEmpty(token.RefreshToken) ? fallbackRefreshToken : token.RefreshToken,
            _timeProvider.GetUtcNow() + lifetime,
            _clientId ?? fallbackClientId);
    }

    private Uri BuildRedirectUri() => new($"http://127.0.0.1:{_options.CallbackPort}/oauth-callback");

    private Uri BuildAuthorizationUrl(string authorizationEndpoint, string challenge, string state, Uri redirectUri)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _clientId!,
            ["redirect_uri"] = redirectUri.ToString(),
            ["response_type"] = "code",
            ["scope"] = _options.Scopes,
            ["state"] = state,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
        };

        var separator = authorizationEndpoint.Contains('?') ? '&' : '?';
        var encoded = string.Join('&', query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        return new Uri($"{authorizationEndpoint}{separator}{encoded}");
    }
}
