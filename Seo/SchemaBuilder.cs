using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using AlSultaanMoving.Data;
using AlSultaanMoving.Models;

namespace AlSultaanMoving.Seo;

public interface ISchemaBuilder
{
    /// <summary>The JSON-LD @graph for the current request, ready to drop inside a script tag.</summary>
    string ForRequest(HttpContext http);
}

/// <summary>
/// Emits one schema.org @graph per page: the business and website nodes on every page,
/// plus whatever the page itself is (FAQPage, BlogPosting, Service, breadcrumbs).
///
/// Deliberately absent: AggregateRating and Review. The four testimonials in
/// content.json are not attributable to verifiable customers, and inventing rating
/// markup is a Google manual-action risk. Add them only from real, sourced reviews.
/// </summary>
public class SchemaBuilder : ISchemaBuilder
{
    private const string BusinessId = "#business";
    private const string WebSiteId = "#website";

    private static readonly string[] AllDays =
    {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
    };

    private static readonly Regex FirstImageSrc = new(
        "<img[^>]+src=[\"']([^\"']+)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Keeps Arabic as literal UTF-8 while still escaping the characters that would be
    // unsafe inside a <script> block. Mirrors the HTML encoder set up in Program.cs.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(
            UnicodeRanges.BasicLatin,
            UnicodeRanges.Arabic,
            UnicodeRanges.ArabicSupplement,
            UnicodeRanges.ArabicExtendedA,
            UnicodeRanges.ArabicPresentationFormsA,
            UnicodeRanges.ArabicPresentationFormsB),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly IContentRepository _repo;
    private readonly ISeoService _seo;

    public SchemaBuilder(IContentRepository repo, ISeoService seo)
    {
        _repo = repo;
        _seo = seo;
    }

    public string ForRequest(HttpContext http)
    {
        var route = http.GetRouteData().Values;
        var controller = (route["controller"] as string ?? "Home").ToLowerInvariant();
        var action = (route["action"] as string ?? "Index").ToLowerInvariant();
        var slug = route["slug"] as string;

        var nodes = new List<object> { Business(), WebSite() };
        nodes.AddRange(PageNodes(controller, action, slug));

        return Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = nodes
        });
    }

    private IEnumerable<object> PageNodes(string controller, string action, string? slug)
    {
        switch (controller, action)
        {
            case ("home", "index"):
                yield return FaqPage();
                break;

            case ("services", "index"):
                yield return Breadcrumb(("خدماتنا", "/services"));
                break;

            case ("services", "details"):
                var service = slug is null ? null : _repo.GetService(slug);
                if (service is not null)
                {
                    yield return ServiceNode(service);
                    yield return Breadcrumb(("خدماتنا", "/services"), (service.Title, "/services/" + service.Slug));
                }
                break;

            case ("blog", "index"):
                yield return BlogNode();
                yield return Breadcrumb(("المدونة", "/blog"));
                break;

            case ("blog", "post"):
                var post = slug is null ? null : _repo.GetPost(slug);
                if (post is not null)
                {
                    yield return BlogPosting(post);
                    yield return Breadcrumb(("المدونة", "/blog"), (post.Title, "/blog/" + post.Slug));
                }
                break;

            case ("areas", "index"):
                yield return Breadcrumb(("مناطق الخدمة", "/areas"));
                break;

            case ("home", "about"):
                yield return Breadcrumb(("من نحن", "/home/about"));
                break;

            case ("contact", "index"):
                yield return Breadcrumb(("تواصل معنا", "/contact"));
                break;
        }
    }

    // ---------------------------------------------------------------- business

    private Dictionary<string, object?> Business()
    {
        var site = _repo.Site;
        var opts = _seo.Options;

        var node = new Dictionary<string, object?>
        {
            ["@type"] = "MovingCompany",
            ["@id"] = _seo.Absolute("/") + BusinessId,
            ["name"] = site.Name,
            ["alternateName"] = Blank(site.ShortName),
            ["description"] = site.Description,
            ["url"] = _seo.Absolute("/"),
            ["telephone"] = site.PhoneIntl,
            ["email"] = Blank(site.Email),
            ["knowsLanguage"] = "ar",
            ["currenciesAccepted"] = "SAR",

            // Service-area business: locality and country only, no street address.
            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["addressLocality"] = site.City,
                ["addressRegion"] = "منطقة الرياض",
                ["addressCountry"] = "SA"
            },

            ["areaServed"] = AreaServed(),
            ["openingHoursSpecification"] = new Dictionary<string, object?>
            {
                ["@type"] = "OpeningHoursSpecification",
                ["dayOfWeek"] = AllDays,
                ["opens"] = "00:00",
                ["closes"] = "23:59"
            },
            ["hasOfferCatalog"] = OfferCatalog()
        };

        if (_seo.LogoUrl is not null) node["logo"] = _seo.LogoUrl;

        var image = _seo.DefaultImageUrl ?? _seo.LogoUrl;
        if (image is not null) node["image"] = image;

        // Each of these stays out of the markup until someone confirms the real value.
        if (!string.IsNullOrWhiteSpace(opts.FoundingYear)) node["foundingDate"] = opts.FoundingYear;
        if (!string.IsNullOrWhiteSpace(opts.PriceRange)) node["priceRange"] = opts.PriceRange;
        if (opts.SameAs.Count > 0) node["sameAs"] = opts.SameAs;

        if (!string.IsNullOrWhiteSpace(opts.CommercialRegistration))
        {
            node["identifier"] = new Dictionary<string, object?>
            {
                ["@type"] = "PropertyValue",
                ["name"] = "السجل التجاري",
                ["value"] = opts.CommercialRegistration
            };
        }

        return node;
    }

    /// <summary>Riyadh, plus the five regions the company actually lists as covered.</summary>
    private List<object> AreaServed()
    {
        var areas = new List<object>
        {
            new Dictionary<string, object?> { ["@type"] = "City", ["name"] = _repo.Site.City }
        };

        areas.AddRange(_repo.GetAreas().Select(a => (object)new Dictionary<string, object?>
        {
            ["@type"] = "Place",
            ["name"] = a.Region
        }));

        return areas;
    }

    private Dictionary<string, object?> OfferCatalog() => new()
    {
        ["@type"] = "OfferCatalog",
        ["name"] = "خدمات نقل الأثاث والعفش",
        ["itemListElement"] = _repo.GetServices().Select(s => new Dictionary<string, object?>
        {
            ["@type"] = "Offer",
            ["itemOffered"] = new Dictionary<string, object?>
            {
                ["@type"] = "Service",
                ["name"] = s.Title,
                ["description"] = s.Short,
                ["url"] = _seo.Absolute("/services/" + s.Slug)
            }
        }).ToList()
    };

    // ---------------------------------------------------------------- pages

    private Dictionary<string, object?> WebSite() => new()
    {
        ["@type"] = "WebSite",
        ["@id"] = _seo.Absolute("/") + WebSiteId,
        ["name"] = _repo.Site.Name,
        ["url"] = _seo.Absolute("/"),
        ["inLanguage"] = "ar",
        ["publisher"] = Ref(BusinessId),
        ["potentialAction"] = new Dictionary<string, object?>
        {
            ["@type"] = "SearchAction",
            ["target"] = new Dictionary<string, object?>
            {
                ["@type"] = "EntryPoint",
                ["urlTemplate"] = _seo.Absolute("/blog") + "?q={search_term_string}"
            },
            ["query-input"] = "required name=search_term_string"
        }
    };

    private Dictionary<string, object?> FaqPage() => new()
    {
        ["@type"] = "FAQPage",
        ["mainEntity"] = _repo.All.Faq.Select(f => new Dictionary<string, object?>
        {
            ["@type"] = "Question",
            ["name"] = f.Q,
            ["acceptedAnswer"] = new Dictionary<string, object?>
            {
                ["@type"] = "Answer",
                ["text"] = f.A
            }
        }).ToList()
    };

    private Dictionary<string, object?> BlogNode() => new()
    {
        ["@type"] = "Blog",
        ["@id"] = _seo.Absolute("/blog") + "#blog",
        ["name"] = "مدونة " + _repo.Site.ShortName,
        ["url"] = _seo.Absolute("/blog"),
        ["inLanguage"] = "ar",
        ["publisher"] = Ref(BusinessId)
    };

    private Dictionary<string, object?> BlogPosting(BlogPost post)
    {
        var url = _seo.Absolute("/blog/" + post.Slug);

        return new Dictionary<string, object?>
        {
            ["@type"] = "BlogPosting",
            ["@id"] = url + "#post",
            ["headline"] = Truncate(post.Title, 110),
            ["description"] = Blank(post.Excerpt),
            ["url"] = url,
            ["mainEntityOfPage"] = url,
            ["inLanguage"] = "ar",
            ["datePublished"] = IsoDate(post.Date),
            ["dateModified"] = IsoDate(post.Modified) ?? IsoDate(post.Date),
            ["wordCount"] = post.WordCount > 0 ? post.WordCount : null,
            ["author"] = Ref(BusinessId),
            ["publisher"] = Ref(BusinessId),
            // Posts carry no images of their own since the dead WordPress ones were
            // removed, so fall back to the site's share image when there is one.
            ["image"] = FirstImage(post.ContentHtml) ?? _seo.DefaultImageUrl
        };
    }

    private Dictionary<string, object?> ServiceNode(Service service)
    {
        var url = _seo.Absolute("/services/" + service.Slug);

        return new Dictionary<string, object?>
        {
            ["@type"] = "Service",
            ["@id"] = url + "#service",
            ["name"] = service.Title,
            ["description"] = service.Short,
            ["url"] = url,
            ["serviceType"] = service.Title,
            ["image"] = service.Image is null ? null : _seo.Absolute(service.Image.Src),
            ["provider"] = Ref(BusinessId),
            ["areaServed"] = AreaServed(),
            ["availableChannel"] = new Dictionary<string, object?>
            {
                ["@type"] = "ServiceChannel",
                ["serviceUrl"] = url,
                ["servicePhone"] = _repo.Site.PhoneIntl
            }
        };
    }

    private Dictionary<string, object?> Breadcrumb(params (string Name, string Path)[] trail)
    {
        var items = new List<object> { BreadcrumbItem(1, "الرئيسية", "/") };

        for (var i = 0; i < trail.Length; i++)
            items.Add(BreadcrumbItem(i + 2, trail[i].Name, trail[i].Path));

        return new Dictionary<string, object?>
        {
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = items
        };
    }

    private Dictionary<string, object?> BreadcrumbItem(int position, string name, string path) => new()
    {
        ["@type"] = "ListItem",
        ["position"] = position,
        ["name"] = name,
        ["item"] = _seo.Absolute(path)
    };

    // ---------------------------------------------------------------- helpers

    private Dictionary<string, object?> Ref(string fragment) => new()
    {
        ["@id"] = _seo.Absolute("/") + fragment
    };

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? IsoDate(string? raw) =>
        DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d)
            ? d.ToString("yyyy-MM-dd")
            : null;

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max].TrimEnd();

    /// <summary>
    /// Pulls the first image out of a post body so BlogPosting carries one.
    /// Returns null when the post has none, rather than inventing a fallback.
    /// </summary>
    private static string? FirstImage(string? html)
    {
        if (string.IsNullOrEmpty(html)) return null;

        var match = FirstImageSrc.Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);
}
