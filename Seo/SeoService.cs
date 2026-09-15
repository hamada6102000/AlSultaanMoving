using Microsoft.Extensions.Options;

namespace AlSultaanMoving.Seo;

public interface ISeoService
{
    SeoOptions Options { get; }

    /// <summary>Absolute URL for a site-relative path.</summary>
    string Absolute(string path);

    /// <summary>The one true URL for the current request, duplicates collapsed.</summary>
    string Canonical(HttpContext http);

    /// <summary>Absolute logo/share-image URLs, or null when the file is not on disk yet.</summary>
    string? LogoUrl { get; }
    string? DefaultImageUrl { get; }
}

/// <summary>
/// Builds canonical and absolute URLs, and resolves which image assets are actually
/// present. Image existence is probed once at startup (same lifetime as the content
/// repository) so a missing file is simply omitted rather than emitted as a 404 URL.
/// </summary>
public class SeoService : ISeoService
{
    public SeoOptions Options { get; }
    public string? LogoUrl { get; }
    public string? DefaultImageUrl { get; }

    public SeoService(IOptions<SeoOptions> options, IWebHostEnvironment env, ILogger<SeoService> logger)
    {
        Options = options.Value;
        Options.BaseUrl = (Options.BaseUrl ?? "").TrimEnd('/');

        if (string.IsNullOrWhiteSpace(Options.BaseUrl))
            logger.LogWarning("Seo:BaseUrl is not set — canonical, Open Graph and sitemap URLs will be relative and Google will ignore them.");

        LogoUrl = ResolveAsset(env, Options.LogoPath, logger);
        DefaultImageUrl = ResolveAsset(env, Options.DefaultImagePath, logger);
    }

    private string? ResolveAsset(IWebHostEnvironment env, string? relativePath, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        var onDisk = Path.Combine(env.WebRootPath ?? "", relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(onDisk)) return Absolute(relativePath);

        logger.LogInformation("SEO asset not found, so it will be omitted from the markup: {Path}", relativePath);
        return null;
    }

    public string Absolute(string path)
    {
        if (string.IsNullOrEmpty(path)) path = "/";
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path;

        if (!path.StartsWith('/')) path = "/" + path;

        // Canonical overrides may carry a query string (blog pagination). Only the
        // path segments get percent-encoded; the query is already in wire form.
        var split = path.IndexOf('?');
        var query = split < 0 ? "" : path[split..];
        if (split >= 0) path = path[..split];

        return Options.BaseUrl + Encode(path) + query;
    }

    public string Canonical(HttpContext http) => Absolute(NormalizePath(http.Request.Path.Value));

    /// <summary>
    /// ASP.NET routing is case-insensitive and treats the default action as optional,
    /// so "/", "/Home", "/Home/Index" and "/home/index" all serve the home page, and
    /// "/Services" and "/services" both serve the service list. Canonicals must agree
    /// on one spelling, so everything is folded to a lowercase, index-free path.
    /// </summary>
    public static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";

        path = path.ToLowerInvariant().TrimEnd('/');

        if (path.EndsWith("/index", StringComparison.Ordinal))
            path = path[..^"/index".Length];

        return path is "" or "/home" ? "/" : path;
    }

    /// <summary>
    /// Percent-encodes each path segment. The blog uses Arabic slugs, which must be
    /// encoded in a canonical/sitemap URL to be valid.
    /// </summary>
    public static string Encode(string path) =>
        string.Join('/', path.Split('/').Select(Uri.EscapeDataString));
}
