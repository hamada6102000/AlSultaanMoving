using System.Globalization;
using AlSultaanMoving.Data;

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

// Emit real UTF-8 Arabic in rendered HTML instead of numeric HTML entities.
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options =>
{
    options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(
        System.Text.Unicode.UnicodeRanges.BasicLatin,
        System.Text.Unicode.UnicodeRanges.Arabic,
        System.Text.Unicode.UnicodeRanges.ArabicSupplement,
        System.Text.Unicode.UnicodeRanges.ArabicExtendedA,
        System.Text.Unicode.UnicodeRanges.ArabicPresentationFormsA,
        System.Text.Unicode.UnicodeRanges.ArabicPresentationFormsB);
});

// Data layer — JSON-backed today, swappable for EF Core (see README).
builder.Services.AddSingleton<IContentRepository, JsonContentRepository>();
builder.Services.AddSingleton<IMessageStore, JsonMessageStore>();

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

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

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
