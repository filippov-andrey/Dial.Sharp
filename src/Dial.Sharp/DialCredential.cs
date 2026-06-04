namespace Dial.Sharp;

/// <summary>Provides DIAL API credentials.</summary>
public sealed class DialCredential
{
    private DialCredential(DialCredentialKind kind, string value)
    {
        Kind = kind;
        Value = value;
    }

    public DialCredentialKind Kind { get; }

    public string Value { get; }

    public static DialCredential ApiKey(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        return new(DialCredentialKind.ApiKey, apiKey);
    }

    public static DialCredential BearerToken(string bearerToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);
        return new(DialCredentialKind.BearerToken, bearerToken);
    }
}

public enum DialCredentialKind
{
    ApiKey,
    BearerToken,
}