using System.Text;
using AlSultaanMoving.Data;
using AlSultaanMoving.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlSultaanMoving.Controllers;

public class ContactController : Controller
{
    private readonly IContentRepository _repo;
    private readonly IConfiguration _config;

    public ContactController(IContentRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
    }

    public IActionResult Index()
    {
        var vm = new ContactViewModel
        {
            Form = new ContactMessage(),
            ServiceTypes = _repo.GetServiceTypes().ToList()
        };
        return View(vm);
    }

    // Handles both the full contact page form and the quick booking form
    // used on the home page and service pages.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Submit(ContactMessage form, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            // Re-render the contact page with validation errors.
            var vm = new ContactViewModel
            {
                Form = form,
                ServiceTypes = _repo.GetServiceTypes().ToList()
            };
            return View("Index", vm);
        }

        // The browser normally submits directly to Apps Script. This server-side path
        // remains a safe fallback if client-side JavaScript is unavailable.
        TempData["Success"] = "تم استلام طلبك بنجاح! سنتواصل معك في أقرب وقت.";
        TempData["WhatsAppUrl"] = BuildWhatsAppUrl(form);
        TempData["EmailUrl"] = BuildEmailUrl(form);

        return RedirectToAction(nameof(ThankYou));
    }

    public IActionResult ThankYou() => View();

    // Builds https://wa.me/<number>?text=<order details> for the configured number.
    private string BuildWhatsAppUrl(ContactMessage form)
    {
        var number = _config["OrderNotifications:WhatsAppNumber"] ?? "";
        number = new string(number.Where(char.IsDigit).ToArray()); // keep digits only

        var text = Uri.EscapeDataString(BuildOrderText(form));
        return $"https://wa.me/{number}?text={text}";
    }

    private string BuildEmailUrl(ContactMessage form)
    {
        // The customer composes this mail themselves, so it must address the public
        // mailbox. The internal OrderNotifications:Email recipient is never exposed.
        var email = _repo.Site.Email;
        var subject = Uri.EscapeDataString("طلب جديد من موقع شركة النور");
        var body = Uri.EscapeDataString(BuildOrderText(form));
        return $"mailto:{email}?subject={subject}&body={body}";
    }

    private static string BuildOrderText(ContactMessage form)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🚚 طلب جديد من موقع شركة النور لنقل الأثاث");
        sb.AppendLine("——————————————");
        sb.AppendLine($"👤 الاسم: {form.Name}");
        sb.AppendLine($"📱 الجوال: {form.Phone}");
        if (!string.IsNullOrWhiteSpace(form.ServiceType))
            sb.AppendLine($"🛠️ الخدمة: {form.ServiceType}");
        if (!string.IsNullOrWhiteSpace(form.Neighborhood))
            sb.AppendLine($"📍 الحي: {form.Neighborhood}");
        if (!string.IsNullOrWhiteSpace(form.Message))
            sb.AppendLine($"📝 التفاصيل: {form.Message}");
        sb.AppendLine($"🕐 التاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");
        return sb.ToString();
    }
}
