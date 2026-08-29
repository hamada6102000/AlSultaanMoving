namespace AlSultaanMoving.Models;

public class HomeViewModel
{
    public SiteContent Content { get; set; } = new();
    public List<BlogPost> LatestPosts { get; set; } = new();
    public ContactMessage Form { get; set; } = new();
}

public class ContactViewModel
{
    public ContactMessage Form { get; set; } = new();
    public List<string> ServiceTypes { get; set; } = new();
    public bool Submitted { get; set; }
}

public class ServiceDetailsViewModel
{
    public Service Service { get; set; } = new();
    public List<Service> Others { get; set; } = new();
    public ContactMessage Form { get; set; } = new();
    public List<string> ServiceTypes { get; set; } = new();
}

public class BlogIndexViewModel
{
    public List<BlogPost> Posts { get; set; } = new();
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public string? Query { get; set; }
}

public class BlogPostViewModel
{
    public BlogPost Post { get; set; } = new();
    public List<BlogPost> Related { get; set; } = new();
}
