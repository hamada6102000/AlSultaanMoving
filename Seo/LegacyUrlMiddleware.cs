using System.Text.RegularExpressions;
using AlSultaanMoving.Data;

namespace AlSultaanMoving.Seo;

/// <summary>
/// Maps URLs left over from the WordPress site onto their current equivalents, and
/// folds the duplicate spellings of a live URL onto one canonical form.
///
/// Rules, in order, each resolving in a SINGLE hop so no redirect chains form:
///   1. /?p={id} and /?page_id={id}  -> the post with that WordPress id (301)
///      /?s={term}                   -> /blog?q={term}                    (301)
///   2. /{post-slug}[/]              -> /blog/{post-slug}                (301)
///   3. old WordPress pages, archives
///      and pagination               -> their closest current page       (301)
///   4. WordPress plumbing paths     -> 410 Gone
///   5. trailing slash, upper case,
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

    /// <summary>
    /// WordPress pages and unmigrated posts that have a clear equivalent here, keyed by
    /// the decoded old path without slashes. Seeded from the Search Console 404 report;
    /// add a line here whenever that report surfaces another old URL.
    /// </summary>
    private static readonly Dictionary<string, string> MovedPages = new(StringComparer.OrdinalIgnoreCase)
    {
        // Site pages
        ["about"] = "/home/about",
        ["about-us"] = "/home/about",
        ["contact-us"] = "/contact",
        ["تواصل-معنا"] = "/contact",
        ["مناطق-الخدمة"] = "/areas",
        ["المدونة"] = "/blog",
        ["سلطان"] = "/",
        ["السلطان"] = "/",

        // Service pages
        ["فك-وتركيب-اثاث-بالرياض"] = "/services/assembly",
        ["خدمة-تخزين-أثاث-بالرياض"] = "/services/storage",

        // Posts that were not migrated, each sent to the closest surviving page
        ["أسرع-شركة-نقل-عفش-بالرياض-مع-الضمان"] = "/blog/أسرع-شركة-نقل-عفش-في-الرياض-مع-الضمان",
        ["ارخص-شركة-نقل-عفش-بالرياض-مع-الفك-والتر"] = "/blog/ارخص-شركة-نقل-عفش-بالرياض-مع-عروض-وخصوم",
        ["مزايا-شركة-نقل-عفش-بالرياض-مع-الفك-والت"] = "/blog/شركة-نقل-عفش-بالرياض",
        ["أفضل-شركة-نقل-عفش-بالرياض-مع-التغليف"] = "/services/packing",
        ["أفضل-خدمة-نقل-اثاث-بالرياض-مع-شركة-السل"] = "/services/home-moving",

        // WordPress and Yoast sitemaps
        ["wp-sitemap.xml"] = "/sitemap.xml",
        ["sitemap_index.xml"] = "/sitemap.xml",
        ["post-sitemap.xml"] = "/sitemap.xml",
        ["page-sitemap.xml"] = "/sitemap.xml"
    };

    /// <summary>/page/2, /المدونة/page/2 and /blog/page/2 -> /blog?page=2.</summary>
    private static readonly Regex Pagination =
        new(@"^(?:(?:المدونة|blog)/)?page/(\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Category, tag, author and date archives, all of which were just lists of posts.</summary>
    private static readonly Regex Archive =
        new(@"^(?:(?:category|tag|author)(?:/.*)?|\d{4}(?:/\d{1,2}){0,2})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>WordPress slugs made from numeric titles (/1302-2/). They carried no content.</summary>
    private static readonly Regex NumericSlug = new(@"^\d+-\d+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

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

        // WordPress site search, e.g. /?s=نقل
        if (path == "/" && request.Query.TryGetValue("s", out var search) && !string.IsNullOrWhiteSpace(search))
        {
            Redirect(context, "/blog", QueryString.Create("q", search.ToString()));
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

        // 3. Old WordPress pages, archives and pagination. Matched on the slash-trimmed
        //    path so /about/ reaches /home/about in one hop, not via /about.
        if (MovedPages.TryGetValue(trimmed, out var moved))
        {
            Redirect(context, moved, QueryString.Empty);
            return;
        }

        var pagination = Pagination.Match(trimmed);
        if (pagination.Success)
        {
            var query = int.TryParse(pagination.Groups[1].Value, out var page) && page > 1
                ? QueryString.Create("page", page.ToString())
                : QueryString.Empty;
            Redirect(context, "/blog", query);
            return;
        }

        if (Archive.IsMatch(trimmed))
        {
            Redirect(context, "/blog", QueryString.Empty);
            return;
        }

        // 4. WordPress plumbing, including the deleted /wp-content/uploads images.
        if (IsGone(path) || NumericSlug.IsMatch(trimmed))
        {
            context.Response.StatusCode = StatusCodes.Status410Gone;
            return;
        }

        if (string.Equals(path, "/index.php", StringComparison.OrdinalIgnoreCase))
        {
            Redirect(context, "/", QueryString.Empty);
            return;
        }

        // 5. One spelling per URL: lower case, no trailing slash, no /home or /index.
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
