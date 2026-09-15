using AlSultaanMoving.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AlSultaanMoving.Seo;

/// <summary>
/// Resolves the title/description/robots values for a page from ViewData, so the
/// layout and the SEO head partial always agree on them.
///
/// Views set one of:
///   ViewData["Title"]       short name, gets " | &lt;site name&gt;" appended
///   ViewData["TitleFull"]   the complete title, used verbatim (home page, paginated lists)
///   ViewData["Description"] page-specific meta description
///   ViewData["Robots"]      e.g. "noindex, follow" for pages that must stay out of the index
///   ViewData["Canonical"]   site-relative path override, for paginated/filtered URLs
/// </summary>
public static class PageMeta
{
    /// <summary>Google truncates around 155-160 characters.</summary>
    public const int DescriptionLimit = 158;

    public static string Title(ViewDataDictionary viewData, SiteInfo site)
    {
        if (viewData["TitleFull"] is string full && !string.IsNullOrWhiteSpace(full))
            return full;

        if (viewData["Title"] is string title && !string.IsNullOrWhiteSpace(title))
            return title + " | " + site.Name;

        return site.Name;
    }

    public static string Description(ViewDataDictionary viewData, SiteInfo site)
    {
        var text = viewData["Description"] as string;
        if (string.IsNullOrWhiteSpace(text)) text = site.Description;

        return Shorten(text, DescriptionLimit);
    }

    public static string? Robots(ViewDataDictionary viewData) => viewData["Robots"] as string;

    public static string? CanonicalOverride(ViewDataDictionary viewData) => viewData["Canonical"] as string;

    /// <summary>
    /// Cuts to a whole word so a description never ends mid-word. The migrated
    /// WordPress excerpts are all exactly 181 characters, so every blog post hits this.
    /// </summary>
    public static string Shorten(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        text = text.Trim();
        if (text.Length <= max) return text;

        var cut = text[..max];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > max / 2) cut = cut[..lastSpace];

        return cut.TrimEnd(' ', '،', ',', '.', '-', '—') + "…";
    }
}
