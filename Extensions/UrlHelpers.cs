using System;
using Microsoft.AspNetCore.Http;

namespace GhanaHybridRentalApi.Extensions;

public static class UrlHelpers
{
    /// <summary>
    /// Return an absolute URL for a stored file path or URL.
    /// - If input is already absolute and matches current request scheme, it is returned as-is.
    /// - If input is absolute but is http while request is https, the scheme is upgraded to https.
    /// - If input is a relative path (starts with '/'), it will be combined with request.Scheme and request.Host.
    /// - If input is a relative path without leading '/', a '/' will be inserted.
    /// </summary>
    public static string AbsolutizeUrl(this HttpRequest request, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url ?? string.Empty;

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            try
            {
                var uri = new Uri(url);
                if (string.Equals(request.Scheme, "https", StringComparison.OrdinalIgnoreCase) && uri.Scheme == Uri.UriSchemeHttp)
                {
                    var b = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps };
                    // If new scheme is https and port was 80 set to default (-1)
                    if (b.Port == 80) b.Port = -1;
                    return b.Uri.ToString();
                }

                return url;
            }
            catch
            {
                // Fall through to constructing absolute URL
            }
        }

        // Relative path
        if (url.StartsWith("/"))
            return $"{request.Scheme}://{request.Host}{url}";

        return $"{request.Scheme}://{request.Host}/{url}";
    }
}