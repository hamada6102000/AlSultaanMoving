namespace AlSultaanMoving.Seo;

/// <summary>
/// Bound from the "Seo" section of appsettings.json.
///
/// Every nullable/empty member here is a business fact we have not confirmed yet.
/// The schema and meta tags emit each one ONLY when it is filled in, so an unknown
/// value stays out of the markup instead of being guessed. Filling any of these in
/// is a config edit — no code change needed.
/// </summary>
public class SeoOptions
{
    /// <summary>Absolute origin, no trailing slash, e.g. https://al-sultaan.com</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>Site-relative paths. Emitted only if the file actually exists on disk.</summary>
    public string LogoPath { get; set; } = "/img/logo.png";
    public string DefaultImagePath { get; set; } = "/img/og-default.jpg";

    // ---- Unconfirmed business facts: leave empty until verified. ----
    public string? FoundingYear { get; set; }
    public string? PriceRange { get; set; }
    public string? CommercialRegistration { get; set; }

    /// <summary>Google Business Profile URL + social profiles, for schema sameAs.</summary>
    public List<string> SameAs { get; set; } = new();
}
