using System.ComponentModel.DataAnnotations;

namespace AlSultaanMoving.Models;

// A submission from the contact / booking form.
public class ContactMessage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "الرجاء إدخال الاسم")]
    [Display(Name = "الاسم")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "الرجاء إدخال رقم الجوال")]
    [RegularExpression(@"^0?5\d{8}$|^\+?9665\d{8}$",
        ErrorMessage = "رقم جوال سعودي غير صحيح (مثال: 05xxxxxxxx)")]
    [Display(Name = "رقم الجوال")]
    public string Phone { get; set; } = "";

    [Display(Name = "نوع الخدمة")]
    public string? ServiceType { get; set; }

    [Display(Name = "الحي / المنطقة")]
    public string? Neighborhood { get; set; }

    [Display(Name = "تفاصيل إضافية")]
    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
