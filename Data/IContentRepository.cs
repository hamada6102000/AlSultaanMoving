using AlSultaanMoving.Models;

namespace AlSultaanMoving.Data;

/// <summary>
/// Read access to the site's content. The JSON-backed implementation ships by
/// default so the project builds and runs with zero external packages. To move
/// to a real database, add EF Core (see README) and provide an
/// EfContentRepository : IContentRepository — nothing else in the app changes.
/// </summary>
public interface IContentRepository
{
    SiteContent All { get; }
    SiteInfo Site { get; }

    IReadOnlyList<Service> GetServices();
    Service? GetService(string slug);

    IReadOnlyList<ServiceArea> GetAreas();

    IReadOnlyList<BlogPost> GetPosts();
    BlogPost? GetPost(string slug);
    IReadOnlyList<BlogPost> GetLatestPosts(int count);
    (IReadOnlyList<BlogPost> Items, int TotalPages) GetPostsPage(int page, int pageSize, string? query = null);

    AboutInfo About { get; }
    IReadOnlyList<FaqItem> GetFaq();
    IReadOnlyList<Testimonial> GetTestimonials();
    IReadOnlyList<string> GetServiceTypes();
}
