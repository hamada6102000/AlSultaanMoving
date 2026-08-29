# شركة السلطان لنقل الأثاث — موقع ASP.NET Core MVC

نسخة من موقع **al-sultaan.com** (شركة نقل أثاث بالرياض) مُعاد بناؤها كمشروع **ASP.NET Core MVC (.NET 8)** ديناميكي، مع ترحيل المحتوى العربي الحقيقي (الصفحات، الخدمات، و24 مقالة من المدونة) من قاعدة بيانات ووردبريس الأصلية.

> An ASP.NET Core MVC (.NET 8) rebuild of the WordPress site *al-sultaan.com* (a Riyadh furniture-moving company), with the real Arabic content migrated from the original WordPress database.

---

## المتطلبات / Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (أو أحدث)
- اتصال إنترنت لعرض الأيقونات والخطوط (Font Awesome + Google Fonts عبر CDN)

## التشغيل / Run

```bash
cd AlSultaanMoving
dotnet run
```

ثم افتح المتصفح على العنوان الذي تطبعه الأداة (عادةً `http://localhost:5xxx`).

أو افتح `AlSultaanMoving.csproj` في **Visual Studio 2022** واضغط **F5**.

> ملاحظة: في هذه البيئة أُضيف ملف `NuGet.config` يُعطّل مصادر NuGet الخارجية. المشروع لا يعتمد على أي حزمة خارجية حالياً، لكن **إذا أردت إضافة حِزم (مثل Entity Framework Core) احذف هذا الملف أولاً** حتى يستطيع NuGet الوصول للإنترنت.

---

## بنية المشروع / Project structure

```
AlSultaanMoving/
├── Program.cs                 # إعداد التطبيق، الثقافة العربية، المسارات (routes)
├── Controllers/               # Home, Services, Blog, Areas, Contact
├── Models/                    # نماذج المحتوى + ContactMessage + ViewModels
├── Data/                      # طبقة الوصول للبيانات (قابلة للاستبدال بـ EF Core)
│   ├── IContentRepository.cs
│   ├── JsonContentRepository.cs   # يقرأ App_Data/content.json عند الإقلاع
│   ├── IMessageStore.cs
│   └── JsonMessageStore.cs        # يحفظ رسائل نموذج التواصل
├── Views/                     # صفحات Razor (RTL عربي)
│   ├── Shared/_Layout.cshtml      # الهيدر/الفوتر/أزرار واتساب العائمة
│   ├── Shared/_BookingForm.cshtml # نموذج الحجز المُعاد استخدامه
│   ├── Home/ Services/ Blog/ Areas/ Contact/
├── wwwroot/
│   ├── css/site.css               # نظام التصميم الكامل (ألوان، مكونات)
│   └── lib/bootstrap/             # Bootstrap 5 RTL (محلي)
└── App_Data/
    ├── content.json               # كل محتوى الموقع (مُرحّل من ووردبريس)
    └── messages.json              # رسائل نموذج التواصل (تُنشأ تلقائياً)
```

## الصفحات / Pages

| المسار | الوصف |
|--------|-------|
| `/` | الرئيسية (هيرو + نموذج حجز + خدمات + مناطق + إحصائيات + آراء + FAQ + مدونة) |
| `/Services` و `/services/{slug}` | قائمة الخدمات وصفحة تفاصيل كل خدمة |
| `/Blog` و `/blog/{slug}` | المدونة (24 مقالة) وصفحة المقال + بحث |
| `/Areas` | مناطق الخدمة (أحياء الرياض) |
| `/Home/About` | من نحن |
| `/Contact` | تواصل معنا + نموذج يعمل فعلياً |

---

## طبقة البيانات / Data layer

المشروع **ديناميكي بالكامل**: لا يوجد أي محتوى مكتوب داخل صفحات العرض. كل شيء يُقرأ من `App_Data/content.json` عبر واجهة `IContentRepository`، ورسائل نموذج التواصل تُحفظ عبر `IMessageStore`.

- **من أين جاء المحتوى؟** تم استخراجه برمجياً من ملف قاعدة بيانات ووردبريس (`.sql`): 24 مقالة بمحتواها الكامل (HTML نظيف)، عناوين الصفحات والخدمات ونصوصها، مع بيانات الشركة (الهاتف، البريد، الإحصائيات) من الموقع المباشر.
- **لتعديل المحتوى** (نص، خدمة، هاتف، مقالة…): عدّل `App_Data/content.json` وأعد تشغيل التطبيق.
- **رسائل نموذج التواصل** تُحفظ في `App_Data/messages.json`.

### لماذا JSON وليس قاعدة بيانات؟

بُني المشروع في بيئة سحابية معزولة **لا تستطيع تحميل حِزم NuGet**، لذا استُخدمت طبقة بيانات مبنية على JSON تعمل بلا أي حزمة خارجية، وصُمِّمت لتُستبدل بـ Entity Framework Core بخطوة واحدة.

---

## الترقية إلى قاعدة بيانات حقيقية (EF Core + SQL Server / SQLite)

الكود مُهيّأ لهذا: كل الوصول للبيانات يمر عبر `IContentRepository` و`IMessageStore`. للتبديل إلى قاعدة بيانات فعلية:

1. **احذف `NuGet.config`** (حتى يصل NuGet للإنترنت)، ثم أضف الحِزم:
   ```bash
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   # أو للـ SQLite:
   dotnet add package Microsoft.EntityFrameworkCore.Sqlite
   dotnet add package Microsoft.EntityFrameworkCore.Design
   ```
2. أنشئ `AppDbContext : DbContext` مع `DbSet<BlogPost>`, `DbSet<Service>`, `DbSet<ContactMessage>` ...
3. أنشئ `EfContentRepository : IContentRepository` و`EfMessageStore : IMessageStore` تستخدمان `AppDbContext`.
4. في `Program.cs` بدّل التسجيل:
   ```csharp
   builder.Services.AddDbContext<AppDbContext>(o =>
       o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
   builder.Services.AddScoped<IContentRepository, EfContentRepository>();
   builder.Services.AddScoped<IMessageStore, EfMessageStore>();
   ```
5. أنشئ الهجرة واملأ البيانات من `content.json`:
   ```bash
   dotnet ef migrations add Init
   dotnet ef database update
   ```

**لا يحتاج أي كنترولر أو صفحة عرض لأي تعديل** — الواجهات تبقى كما هي.

---

## ملاحظات / Notes

- **الأيقونات والخطوط** تُحمّل من CDN (Font Awesome + Google Fonts Cairo). تحتاج اتصال إنترنت لعرضها؛ بدونه يعمل الموقع لكن تظهر الأيقونات كمربّعات.
- **آراء العملاء** في الصفحة الرئيسية هي **نماذج توضيحية** — استبدلها بمراجعات حقيقية في `content.json` قبل النشر.
- تم بناء التصميم من الصفر (لم يُنسخ HTML ووردبريس/Elementor) لإنتاج كود نظيف وقابل للصيانة، مع الحفاظ على نفس الأقسام والهوية العربية RTL.
- الموقع الأصلي كان يستخدم صوراً افتراضية من قالب Jannah، لذا اعتمد هذا الإصدار على تصميم CSS وأيقونات بدلاً من الصور.
