using AlSultaanMoving.Data;

namespace AlSultaanMoving.Seo;

/// <summary>
/// Maps URLs left over from the WordPress site onto their current equivalents, and
/// folds the duplicate spellings of a live URL onto one canonical form.
///
/// Rules, in order, each resolving in a SINGLE hop so no redirect chains form:
///   1. /?p={id} and /?page_id={id}  -> the post with that WordPress id (301)
///   2. /{post-slug}[/]              -> /blog/{post-slug}                (301)
///   3. WordPress plumbing paths     -> 410 Gone
///   4. trailing slash, upper case,
///      /home, /home/index          -> the normalised path               (301)
///
/// Anything else falls through to a normal 404 on purpose. Redirecting unknown URLs
/// to the home page would make Google treat them as soft 404s and can suppress the
/// destination, which is worse than an honest 404.
/// </summary>
public class LegacyUrlMiddleware
{
    // Requests for these are permanently gone rather than moved. 410 tells Google to
    // drop them faster than a 404 does, and they have no equivalent on this site.
    private static readonly string[] GonePrefixes =
    {
        "/wp-content/", "/wp-includes/", "/wp-json/", "/wp-admin"
    };

    private static readonly string[] GoneExact =
    {
        "/wp-login.php", "/xmlrpc.php", "/wp-cron.php", "/wp-links-opml.php",
        "/feed", "/rss", "/rss2", "/comments/feed", "/trackback"
    };

    private readonly RequestDelegate _next;

    /// <summary>Decoded post slug -> current path. Built once from the content file.</summary>
    private readonly Dictionary<string, string> _bySlug;

    /// <summary>WordPress numeric post id -> current path.</summary>
    private readonly Dictionary<string, string> _byWordPressId;

    public LegacyUrlMiddleware(RequestDelegate next, IContentRepository repo)
    {
        _next = next;

        _bySlug = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _byWordPressId = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var post in repo.GetPosts())
        {
            if (string.IsNullOrWhiteSpace(post.Slug)) continue;

            var target = "/blog/" + post.Slug;
            _bySlug[post.Slug] = target;
            if (post.Id > 0) _byWordPressId[post.Id.ToString()] = target;
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        // Never redirect a form post — that would discard the body.
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            await _next(context);
            return;
        }

        var path = request.Path.Value ?? "/";

        // 1. WordPress ugly permalinks, e.g. /?p=1138
        if (request.Query.TryGetValue("p", out var p) && _byWordPressId.TryGetValue(p.ToString(), out var byId))
        {
            Redirect(context, byId, QueryString.Empty);
            return;
        }

        if (request.Query.TryGetValue("page_id", out var pageId) &&
            _byWordPressId.TryGetValue(pageId.ToString(), out var byPageId))
        {
            Redirect(context, byPageId, QueryString.Empty);
            return;
        }

        // 2. The old permalink was /{slug}; posts now live under /blog/{slug}.
        //    Only single-segment paths qualify, so /blog/{slug} can never re-match
        //    and bounce the request a second time.
        var trimmed = path.Trim('/');
        if (trimmed.Length > 0 && !trimmed.Contains('/') && _bySlug.TryGetValue(trimmed, out var bySlug))
        {
            Redirect(context, bySlug, QueryString.Empty);
            return;
        }

        // 3. WordPress plumbing, including the deleted /wp-content/uploads images.
        if (IsGone(path))
        {
            context.Response.StatusCode = StatusCodes.Status410Gone;
            return;
        }

        if (string.Equals(path, "/index.php", StringComparison.OrdinalIgnoreCase))
        {
            Redirect(context, "/", QueryString.Empty);
            return;
        }

        // 4. One spelling per URL: lower case, no trailing slash, no /home or /index.
        var normalised = SeoService.NormalizePath(path);
        if (!string.Equals(normalised, path, StringComparison.Ordinal))
        {
            Redirect(context, normalised, request.QueryString);
            return;
        }

        await _next(context);
    }

    private static bool IsGone(string path)
    {
        var trimmed = path.TrimEnd('/');

        foreach (var prefix in GonePrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        }

        foreach (var exact in GoneExact)
        {
            if (string.Equals(trimmed, exact, StringComparison.OrdinalIgnoreCase)) return true;
        }

        // /anything/feed and /anything/trackback, which WordPress generated per post.
        return trimmed.EndsWith("/feed", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("/trackback", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 301 to a site-relative, percent-encoded path. Relative keeps the redirect
    /// correct on localhost and behind any host name, and the blog's Arabic slugs
    /// must be encoded to be a valid Location value.
    /// </summary>
    private static void Redirect(HttpContext context, string path, QueryString query)
    {
        context.Response.Headers.Location = SeoService.Encode(path) + query.ToUriComponent();
        context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
    }
}

public static class LegacyUrlMiddlewareExtensions
{
    public static IApplicationBuilder UseLegacyUrlRedirects(this IApplicationBuilder app) =>
        app.UseMiddleware<LegacyUrlMiddleware>();
}
