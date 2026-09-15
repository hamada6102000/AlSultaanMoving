using System.Globalization;
using System.IO.Compression;
using AlSultaanMoving.Data;
using AlSultaanMoving.Seo;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;

// Locate the project folder (the one containing wwwroot + App_Data) no matter how
// the app is launched — via `dotnet run`, Visual Studio F5, or by running the
// built .exe/.dll directly from bin\ (which otherwise sets the content root to bin).
static string ResolveContentRoot()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "wwwroot")) &&
                Directory.Exists(Path.Combine(dir.FullName, "App_Data")))
                return dir.FullName;
            dir = dir.Parent;
        }
    }
    return Directory.GetCurrentDirectory();
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = ResolveContentRoot()
});

builder.Services.AddControllersWithViews();

// Generate every link in one spelling. ASP.NET routing is case-insensitive, so without
// this the same page is reachable at /Services and /services and Google sees duplicates.
// Query strings stay untouched so the blog's ?q= search terms are not folded.
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

// Emit real UTF-8 Arabic in rendered HTML instead of numeric HTML entities.
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options =>
{
    options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(
        System.Text.Unicode.UnicodeRanges.BasicLatin,
        // Em dash and ellipsis appear in titles and trimmed meta descriptions; without
        // this range they render as &#x2014; / &#x2026; entities in the markup.
        System.Text.Unicode.UnicodeRanges.GeneralPunctuation,
        System.Text.Unicode.UnicodeRanges.Arabic,
        System.Text.Unicode.UnicodeRanges.ArabicSupplement,
        System.Text.Unicode.UnicodeRanges.ArabicExtendedA,
        System.Text.Unicode.UnicodeRanges.ArabicPresentationFormsA,
        System.Text.Unicode.UnicodeRanges.ArabicPresentationFormsB);
});

// Content is JSON-backed. Orders are sent directly from the browser to Apps Script.
builder.Services.AddSingleton<IContentRepository, JsonContentRepository>();

// Canonical URLs, Open Graph and schema.org markup.
builder.Services.Configure<SeoOptions>(builder.Configuration.GetSection("Seo"));
builder.Services.AddSingleton<ISeoService, SeoService>();
builder.Services.AddSingleton<ISchemaBuilder, SchemaBuilder>();

// Brotli/gzip for HTML, CSS, JS, SVG and the XML sitemap. Images and woff2 are
// already compressed, so they are deliberately left out of the MIME list.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = new[]
    {
        "text/html", "text/css", "text/plain", "text/xml",
        "application/javascript", "text/javascript",
        "application/json", "application/xml",
        "image/svg+xml", "application/manifest+json"
    };
});

builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

var app = builder.Build();

// Arabic (Saudi) culture for numbers/text, but with the Gregorian calendar
// so the Gregorian post dates display correctly (ar-SA defaults to Hijri).
var culture = new CultureInfo("ar-SA");
try { culture.DateTimeFormat.Calendar = new GregorianCalendar(); } catch { /* keep default */ }
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseResponseCompression();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // /lib and /img are versioned by content or never change in place, so they can
        // be held for a year. /css and /js are linked with asp-append-version, which
        // busts the cache on edit, but a shorter window is kept as a safety net for
        // anything that reaches them without the ?v= query.
        var path = ctx.Context.Request.Path.Value ?? "";
        var immutable = path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)
                     || path.StartsWith("/img/", StringComparison.OrdinalIgnoreCase);

        ctx.Context.Response.Headers[HeaderNames.CacheControl] = immutable
            ? "public,max-age=31536000,immutable"
            : "public,max-age=604800";
    }
});

// Old WordPress URLs, plus one-hop canonical normalisation. Runs after static files
// so real assets are served first, and before routing so MVC never sees a stale URL.
app.UseLegacyUrlRedirects();

app.UseRouting();
app.UseAuthorization();

// Crawler files, generated from the content repository so they never go stale.
app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Seo", action = "Sitemap" });

app.MapControllerRoute(
    name: "robots",
    pattern: "robots.txt",
    defaults: new { controller = "Seo", action = "Robots" });

// Friendly slug routes (Arabic slugs are supported).
app.MapControllerRoute(
    name: "service",
    pattern: "services/{slug}",
    defaults: new { controller = "Services", action = "Details" });

app.MapControllerRoute(
    name: "blogpost",
    pattern: "blog/{slug}",
    defaults: new { controller = "Blog", action = "Post" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
