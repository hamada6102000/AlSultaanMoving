using System.Text;
using AlSultaanMoving.Data;
using AlSultaanMoving.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlSultaanMoving.Controllers;

public class ContactController : Controller
{
    private readonly IContentRepository _repo;
    private readonly IMessageStore _messages;
    private readonly IConfiguration _config;

    public ContactController(IContentRepository repo, IMessageStore messages, IConfiguration config)
    {
        _repo = repo;
        _messages = messages;
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
    public async Task<IActionResult> Submit(ContactMessage form, string? returnUrl = null)
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

        // 1) Keep a local record of the order.
        await _messages.AddAsync(form);

        // 2) Build a WhatsApp link that sends all the order details to the business.
        TempData["Success"] = "تم استلام طلبك بنجاح! سنتواصل معك في أقرب وقت.";
        TempData["WhatsAppUrl"] = BuildWhatsAppUrl(form);

        return RedirectToAction(nameof(ThankYou));
    }

    public IActionResult ThankYou() => View();

    // Builds https://wa.me/<number>?text=<order details> for the configured number.
    private string BuildWhatsAppUrl(ContactMessage form)
    {
        var number = _config["OrderNotifications:WhatsAppNumber"] ?? "";
        number = new string(number.Where(char.IsDigit).ToArray()); // keep digits only

        var sb = new StringBuilder();
        sb.AppendLine("🚚 طلب جديد من موقع شركة السلطان لنقل الأثاث");
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

        var text = Uri.EscapeDataString(sb.ToString());
        return $"https://wa.me/{number}?text={text}";
    }
}
