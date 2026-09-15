using System.Text;
using System.Xml;
using System.Xml.Linq;
using AlSultaanMoving.Data;
using AlSultaanMoving.Seo;
using Microsoft.AspNetCore.Mvc;

namespace AlSultaanMoving.Controllers;

/// <summary>
/// Serves /sitemap.xml and /robots.txt. The sitemap is generated from the content
/// repository on every request, so new services and blog posts appear in it as soon
/// as they land in content.json — there is no file to remember to regenerate.
/// </summary>
public class SeoController : Controller
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private readonly IContentRepository _repo;
    private readonly ISeoService _seo;

    public SeoController(IContentRepository repo, ISeoService seo)
    {
        _repo = repo;
        _seo = seo;
    }

    [ResponseCache(Duration = 3600)]
    public IActionResult Sitemap()
    {
        var urls = new List<XElement>
        {
            UrlEntry("/"),
            UrlEntry("/services"),
            UrlEntry("/blog"),
            UrlEntry("/areas"),
            UrlEntry("/home/about"),
            UrlEntry("/contact")
        };

        urls.AddRange(_repo.GetServices().Select(s => UrlEntry("/services/" + s.Slug)));

        // Posts carry a real modified date, so they get a lastmod. The static pages
        // have no tracked date and deliberately get none rather than a made-up one.
        urls.AddRange(_repo.GetPosts().Select(p => UrlEntry("/blog/" + p.Slug, LastModified(p.Modified, p.Date))));

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(Ns + "urlset", urls));

        return File(Serialize(document), "application/xml; charset=utf-8");
    }

    [ResponseCache(Duration = 3600)]
    public IActionResult Robots()
    {
        var body = new StringBuilder()
            .AppendLine("User-agent: *")
            .AppendLine("Allow: /")
            .AppendLine()
            .AppendLine("# Thin or transactional pages — also marked noindex in the page head.")
            .AppendLine("Disallow: /contact/thankyou")
            .AppendLine("Disallow: /home/error")
            .AppendLine();

        if (!string.IsNullOrWhiteSpace(_seo.Options.BaseUrl))
            body.AppendLine("Sitemap: " + _seo.Absolute("/sitemap.xml"));

        return Content(body.ToString(), "text/plain; charset=utf-8");
    }

    private XElement UrlEntry(string path, string? lastModified = null)
    {
        var element = new XElement(Ns + "url", new XElement(Ns + "loc", _seo.Absolute(path)));

        if (lastModified is not null)
            element.Add(new XElement(Ns + "lastmod", lastModified));

        return element;
    }

    private static string? LastModified(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (DateTime.TryParse(candidate, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsed))
                return parsed.ToString("yyyy-MM-dd");
        }

        return null;
    }

    private static byte[] Serialize(XDocument document)
    {
        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true };

        using (var writer = XmlWriter.Create(stream, settings))
            document.Save(writer);

        return stream.ToArray();
    }
}
