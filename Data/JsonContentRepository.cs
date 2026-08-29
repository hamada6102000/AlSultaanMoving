using System.Text.Json;
using AlSultaanMoving.Models;

namespace AlSultaanMoving.Data;

/// <summary>
/// Loads the migrated WordPress content from App_Data/content.json once at
/// startup and serves it from memory. Registered as a singleton.
/// </summary>
public class JsonContentRepository : IContentRepository
{
    private readonly SiteContent _data;

    public JsonContentRepository(IWebHostEnvironment env, ILogger<JsonContentRepository> logger)
    {
        var path = Path.Combine(env.ContentRootPath, "App_Data", "content.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            var json = File.ReadAllText(path);
            _data = JsonSerializer.Deserialize<SiteContent>(json, options) ?? new SiteContent();
            // newest first
            _data.Blog = _data.Blog.OrderByDescending(p => p.PublishedDate).ToList();
            logger.LogInformation("Loaded content: {Services} services, {Posts} blog posts",
                _data.Services.Count, _data.Blog.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load content.json from {Path}", path);
            _data = new SiteContent();
        }
    }

    public SiteContent All => _data;
    public SiteInfo Site => _data.Site;

    public IReadOnlyList<Service> GetServices() => _data.Services;
    public Service? GetService(string slug) =>
        _data.Services.FirstOrDefault(s => s.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ServiceArea> GetAreas() => _data.Areas;

    public IReadOnlyList<BlogPost> GetPosts() => _data.Blog;

    public BlogPost? GetPost(string slug)
    {
        if (string.IsNullOrEmpty(slug)) return null;
        var decoded = Uri.UnescapeDataString(slug);
        return _data.Blog.FirstOrDefault(p =>
            p.Slug == slug || p.Slug == decoded ||
            Uri.UnescapeDataString(p.Slug) == decoded);
    }

    public IReadOnlyList<BlogPost> GetLatestPosts(int count) =>
        _data.Blog.Take(count).ToList();

    public (IReadOnlyList<BlogPost> Items, int TotalPages) GetPostsPage(int page, int pageSize, string? query = null)
    {
        IEnumerable<BlogPost> q = _data.Blog;
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(p => p.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                          || p.Excerpt.Contains(query, StringComparison.OrdinalIgnoreCase));
        var list = q.ToList();
        var total = Math.Max(1, (int)Math.Ceiling(list.Count / (double)pageSize));
        page = Math.Clamp(page, 1, total);
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, total);
    }

    public AboutInfo About => _data.About;
    public IReadOnlyList<FaqItem> GetFaq() => _data.Faq;
    public IReadOnlyList<Testimonial> GetTestimonials() => _data.Testimonials;
    public IReadOnlyList<string> GetServiceTypes() => _data.ServiceTypes;
}
