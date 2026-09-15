namespace AlSultaanMoving.Models;

// Root object matching App_Data/content.json
public class SiteContent
{
    public SiteInfo Site { get; set; } = new();
    public List<Service> Services { get; set; } = new();
    public List<ProcessStep> Process { get; set; } = new();
    public List<Feature> Features { get; set; } = new();
    public List<ServiceArea> Areas { get; set; } = new();
    public AboutInfo About { get; set; } = new();
    public List<FaqItem> Faq { get; set; } = new();
    public List<Testimonial> Testimonials { get; set; } = new();
    public List<string> ServiceTypes { get; set; } = new();
    public List<BlogPost> Blog { get; set; } = new();
}

public class SiteInfo
{
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string Description { get; set; } = "";
    public string Phone { get; set; } = "";
    public string PhoneIntl { get; set; } = "";
    public string Whatsapp { get; set; } = "";
    public string Email { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string Hours { get; set; } = "";
    public List<Stat> Stats { get; set; } = new();
}

public class Stat
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}

public class Service
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Short { get; set; } = "";
    public string Content { get; set; } = "";

    /// <summary>
    /// Optional real photograph. Services without one keep the icon-based card design
    /// rather than showing a placeholder, so the grid stays visually consistent.
    /// </summary>
    public ServiceImage? Image { get; set; }
}

public class ServiceImage
{
    public string Src { get; set; } = "";
    public string Alt { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
}

public class ProcessStep
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Desc { get; set; } = "";
}

public class Feature
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Desc { get; set; } = "";
}

public class ServiceArea
{
    public string Region { get; set; } = "";
    public string Icon { get; set; } = "";
    public List<string> Neighborhoods { get; set; } = new();
}

public class AboutInfo
{
    public string Title { get; set; } = "";
    public string Intro { get; set; } = "";
    public string Mission { get; set; } = "";
    public string Vision { get; set; } = "";
    public List<Feature> Values { get; set; } = new();
}

public class FaqItem
{
    public string Q { get; set; } = "";
    public string A { get; set; } = "";
}

public class Testimonial
{
    public string Name { get; set; } = "";
    public string Area { get; set; } = "";
    public int Rating { get; set; } = 5;
    public string Text { get; set; } = "";
}

public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Date { get; set; } = "";
    public string Modified { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public string ContentHtml { get; set; } = "";
    public int WordCount { get; set; }

    public DateTime PublishedDate =>
        DateTime.TryParse(Date, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d) ? d : DateTime.MinValue;
}
