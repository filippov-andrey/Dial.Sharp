namespace Dial.Sharp;

/// <summary>How a <see cref="DialClient"/> attaches authentication to its requests.</summary>
internal enum DialAuthMode
{
    /// <summary>Sends the DIAL <c>Api-Key</c> header.</summary>
    ApiKey,

    /// <summary>Sends a static <c>Authorization: Bearer</c> header.</summary>
    BearerToken,

    /// <summary>No static header; authentication is provided by handlers on the supplied <see cref="HttpClient"/>.</summary>
    External,
}
