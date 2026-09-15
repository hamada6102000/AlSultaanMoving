# شركة النور لنقل الأثاث — موقع ASP.NET Core MVC

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
├── Data/                      # مستودع محتوى الموقع
│   ├── IContentRepository.cs
│   ├── JsonContentRepository.cs   # يقرأ App_Data/content.json عند الإقلاع
├── GoogleAppsScript/          # الكود الذي يُنسخ إلى Google Apps Script
│   └── Code.gs
├── Views/                     # صفحات Razor (RTL عربي)
│   ├── Shared/_Layout.cshtml      # الهيدر/الفوتر/أزرار واتساب العائمة
│   ├── Shared/_BookingForm.cshtml # نموذج الحجز المُعاد استخدامه
│   ├── Home/ Services/ Blog/ Areas/ Contact/
├── wwwroot/
│   ├── css/site.css               # نظام التصميم الكامل (ألوان، مكونات)
│   └── lib/bootstrap/             # Bootstrap 5 RTL (محلي)
└── App_Data/
    ├── content.json               # كل محتوى الموقع (مُرحّل من ووردبريس)
   └── content.json               # كل محتوى الموقع
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

المشروع **ديناميكي بالكامل**: لا يوجد أي محتوى مكتوب داخل صفحات العرض. كل شيء يُقرأ من `App_Data/content.json` عبر واجهة `IContentRepository`. الطلبات تُرسل من نموذج Razor مباشرة إلى Google Apps Script، ولا تُحفظ محلياً في المشروع.

- **من أين جاء المحتوى؟** تم استخراجه برمجياً من ملف قاعدة بيانات ووردبريس (`.sql`): 24 مقالة بمحتواها الكامل (HTML نظيف)، عناوين الصفحات والخدمات ونصوصها، مع بيانات الشركة (الهاتف، البريد، الإحصائيات) من الموقع المباشر.
- **لتعديل المحتوى** (نص، خدمة، هاتف، مقالة…): عدّل `App_Data/content.json` وأعد تشغيل التطبيق.
- **طلبات نموذج التواصل** تُحفظ في Google Sheets عبر Google Apps Script.

## Google Apps Script وإشعارات Telegram

الكود الجاهز موجود في `GoogleAppsScript/Code.gs`. أنشئ Google Sheet، ثم افتح **Extensions → Apps Script** والصق الكود. في **Project Settings → Script Properties** أضف الخصائص التالية:

| Property | Value |
|---|---|
| `TELEGRAM_BOT_TOKEN` | توكن البوت، داخل Apps Script فقط |
| `TELEGRAM_CHAT_ID` | Chat ID الخاص بك، داخل Apps Script فقط |
| `SPREADSHEET_ID` | معرّف Google Sheet |

لا تضع هذه القيم في HTML أو JavaScript أو `appsettings.json`.

انشر المشروع من **Deploy → New deployment → Web app** بهذه الإعدادات:

- **Execute as:** Me
- **Who has access:** Anyone

بعد النشر، يجب أن يطابق رابط Web App قيمة `OrderNotifications:GoogleAppsScriptUrl` في `appsettings.json`.

### أعمدة Google Sheets

يُنشئ الكود الصف الأول تلقائياً إذا كانت الورقة فارغة:

`Timestamp`, `Order ID`, `Date`, `Time`, `Customer Name`, `Phone`, `Service`, `Neighborhood`, `Notes`, `Status`

الحالة الابتدائية لكل طلب هي `New`. رقم الطلب يكون بالشكل `ORD-YYYYMMDD-0001`.

### رسالة Telegram

تصل رسالة عربية بهذا الشكل:

```text
🚚 طلب جديد من موقع شركة النور لنقل الأثاث
🆔 رقم الطلب: ORD-YYYYMMDD-0001
👤 الاسم: ...
📱 الجوال: ...
🛠️ الخدمة: ...
📍 الحي: ...
📝 التفاصيل: ...
🕐 وقت الطلب: YYYY/MM/DD HH:mm
```

للحصول على Chat ID: ابدأ محادثة مع البوت وأرسل له رسالة، ثم افتح `https://api.telegram.org/bot<TOKEN>/getUpdates` مؤقتاً وابحث عن `message.chat.id`. لا تشارك الرابط أو التوكن. بما أن التوكن ظهر في رسالة المحادثة، يوصى بإلغائه من BotFather وإصدار توكن جديد قبل الاستخدام الفعلي.

### الاختبار

اختبر بالترتيب:

1. أرسل طلباً صحيحاً من الموقع وتأكد من ظهور JSON نجاح في Apps Script Execution log.
2. تحقق من إضافة صف في Google Sheet.
3. تحقق من وصول رسالة Telegram.
4. جرّب اسماً فارغاً ورقم جوال غير صحيح.
5. انقر زر الإرسال عدة مرات بسرعة وتأكد من تعطيله أثناء الإرسال.
6. اختبر من نفس النطاق المستضاف فعلياً؛ لا يمكن التحقق من CORS أو Telegram أو Google Sheets من `dotnet build` وحده.

إذا ظهر خطأ CORS في المتصفح، راجع أن النشر هو **Web app / Execute as Me / Anyone** وأن الرابط هو `/exec` وليس `/dev`. لا تستخدم `no-cors` لأن ذلك يمنع الموقع من قراءة استجابة JSON والتحقق من نجاح الطلب.

### لماذا JSON وليس قاعدة بيانات؟

بُني المشروع في بيئة سحابية معزولة **لا تستطيع تحميل حِزم NuGet**، لذا استُخدمت طبقة بيانات مبنية على JSON تعمل بلا أي حزمة خارجية، وصُمِّمت لتُستبدل بـ Entity Framework Core بخطوة واحدة.

---

## الترقية إلى قاعدة بيانات حقيقية (EF Core + SQL Server / SQLite)

الكود مُهيّأ لهذا: محتوى الموقع يمر عبر `IContentRepository`. للتبديل إلى قاعدة بيانات فعلية:

1. **احذف `NuGet.config`** (حتى يصل NuGet للإنترنت)، ثم أضف الحِزم:
   ```bash
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   # أو للـ SQLite:
   dotnet add package Microsoft.EntityFrameworkCore.Sqlite
   dotnet add package Microsoft.EntityFrameworkCore.Design
   ```
2. أنشئ `AppDbContext : DbContext` مع `DbSet<BlogPost>`, `DbSet<Service>`, `DbSet<ContactMessage>` ...
3. أنشئ `EfContentRepository : IContentRepository` يستخدم `AppDbContext`.
4. في `Program.cs` بدّل التسجيل:
   ```csharp
   builder.Services.AddDbContext<AppDbContext>(o =>
       o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
   builder.Services.AddScoped<IContentRepository, EfContentRepository>();
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
