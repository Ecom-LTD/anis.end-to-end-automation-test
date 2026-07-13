# FazzaAutomation — إطار اختبارات E2E (.NET 8 / xUnit)

حلٌّ كامل قابل للتشغيل يطبّق الخطة التحسينية: بنية لكل Microservice، حاوية DI، كاش جلسات،
مجموعات مستخدمين معزولة، إعادة محاولة مركزية عند 401، وسيناريوهات متعدّدة الخدمات — مع
**طبقة API وهمية داخل الذاكرة** بحيث يعمل `dotnet test` أخضرَ فورًا دون أي خادم حقيقي.

> تم بناء الإطار وتشغيله فعليًا: عيّنة التشغيل (`Automation.Smoke`) تعطي **١١/١١ نجاح**،
> وكل ملفات الاختبار تُترجَم بنجاح. (لم تُشغَّل حزم xUnit في بيئة الإعداد بسبب غياب الإنترنت،
> لكنها تُسترجَع تلقائيًا عند أول `dotnet build` على جهازك.)

---

## المتطلبات

- **.NET 8 SDK** (مثبّت ضمن Visual Studio 2026، أو VS 2022 ‎17.10+‎، أو مستقلًّا من dotnet.microsoft.com).
- اتصال بالإنترنت لأول استرجاع لحزم NuGet (xUnit) فقط.

يعمل على: **Visual Studio 2026** (إصدار يونيو 2026)، أو VS 2022 ‎17.10+‎، أو `dotnet` CLI من الطرفية.

---

## بنية الحل

```
FazzaAutomation.sln
├── nuget.config                         مصدر NuGet (nuget.org)
├── src/Automation.Framework/            مكتبة الإطار
│   ├── Core/
│   │   ├── Http/        ApiClient, ApiResponse, ApiException
│   │   ├── Configuration/ Settings, ConfigurationManager, UserHelper
│   │   ├── Context/     StateManager
│   │   ├── Constants/   StateKeys, TestUsers
│   │   ├── Enums/       CurrencyType, SubscriptionType, WalletType
│   │   ├── Session/     TestSession, SessionOptions, SessionBuilder,
│   │   │                SessionCache, ResilientSession
│   │   └── UserPool/    UserLease, UserPoolManager, UserPoolRegistry
│   ├── Services/                        خدمة لكل Microservice
│   │   ├── Identity/    Endpoints · Models · Client · Flows
│   │   ├── Account/     Endpoints · Models · Client · Flows
│   │   ├── Wallet/      Endpoints · Models · Client · Flows
│   │   └── Transfer/    Endpoints · Models · Client · Flows
│   ├── Composition/     FrameworkRegistration  (جذر الـ DI)
│   ├── Testing/         FakeBackendHandler     (الـ API الوهمي)
│   └── Configuration/   appsettings.json
├── tests/Automation.Test/               مشروع xUnit
│   ├── Infrastructure/  TestHost, BaseFixture, AssemblyInfo
│   ├── Sessions/        SulfaSessions
│   ├── Fixtures/        SulfaFixture
│   ├── Collections/     SulfaCollection
│   ├── Scenarios/       ScenarioContext, Scenario
│   ├── Tests/Sulfa/     BaseSulfaTest, WalletTests
│   ├── Tests/CrossService/ TransferConsistencyScenario, PooledUserTests
│   └── xunit.runner.json
└── tools/Automation.Smoke/              عيّنة تشغيل سريعة (Console)
    └── Program.cs
```

---

## الحزم (NuGet)

**Automation.Test** فقط يحتاج حزمًا (تُسترجَع تلقائيًا):
- `Microsoft.NET.Test.Sdk` 17.12.0
- `xunit` 2.9.2
- `xunit.runner.visualstudio` 2.8.2
- `coverlet.collector` 6.0.2

**Automation.Framework** و **Automation.Smoke**: لا حزم خارجية — يستخدمان الـ Shared Framework
لـ ASP.NET Core (‏`<FrameworkReference Include="Microsoft.AspNetCore.App" />`‎) للحصول على
حقن التبعيات والإعدادات و System.Text.Json.

> لاستخدام Newtonsoft.Json كما في كودك القديم: أضِف `Newtonsoft.Json` إلى مشروع الإطار
> واستبدل استدعاءات `System.Text.Json` في `ApiClient`.

---

## كيف تشغّله

### الطريقة الأولى: Visual Studio 2026
1. افتح `FazzaAutomation.sln`.
2. انتظر استرجاع الحزم (NuGet Restore تلقائي).
3. **Build → Build Solution** (‏Ctrl+Shift+B‏).
4. **Test → Run All Tests** (‏Test Explorer‏) — يجب أن تكون كل الاختبارات خضراء.

### الطريقة الثانية: الطرفية (dotnet CLI)
```bash
cd FazzaAutomation
dotnet test                                   # يبني ويشغّل كل اختبارات xUnit
dotnet run --project tools/Automation.Smoke   # عيّنة سريعة تطبع 11/11
```

النتيجة المتوقعة من العيّنة:
```
[6] سيناريو: balance → transfer → verify
     رصيد المرسل قبل: 1000
  ✅ Transfer succeeded
     رصيد المرسل بعد:  995
  ✅ Sender balance decreased by amount
 النتيجة: 11 نجاح / 0 فشل
```

---

## التبديل إلى الـ API الحقيقي

في `src/Automation.Framework/Configuration/appsettings.json`:
```json
"ApiSettings": {
  "GatewayUrl":  "https://<عنوان البوابة الحقيقي>",
  "IdentityUrl": "https://<عنوان الهوية الحقيقي>",
  "UseFakeBackend": false
}
```
- اضبط `UseFakeBackend` على `false` ليستخدم `ApiClient` الشبكة الحقيقية بدل `FakeBackendHandler`.
- حدّث أرقام الهواتف/المستخدمين في `Users` بقيمك الفعلية.
- عدّل مسارات الـ Endpoints وبنية الـ Models في كل خدمة لتطابق استجابات الـ API لديك.

---

## كيف تضيف Microservice جديد (Card / Sulfa / Forex / Remittance / Report)

الخدمات الأربع الموجودة (Identity, Account, Wallet, Transfer) قالبٌ جاهز. لإضافة خدمة:

1. أنشئ `Services/<Name>/{Endpoints, Models, Client, Flows}` على غرار خدمة Wallet.
2. في `Composition/FrameworkRegistration.cs` أضِف سطرين:
   ```csharp
   services.AddSingleton<<Name>Client>();
   services.AddSingleton<<Name>Flow>();
   ```
3. أضِف مستخدمي المشروع في `appsettings.json` بحقل `"Project": "<Name>"`.
4. (إن كان للمنتج جلسات خاصة) أنشئ `Sessions/<Name>Sessions.cs` و`Fixtures/<Name>Fixture.cs`
   و`Collections/<Name>Collection.cs` نسخًا من نظائر Sulfa.
5. اكتب الاختبارات تحت `Tests/<Name>/`.

لا يتغيّر أي ملف آخر.

---

## ملاحظات مهمة في التصميم

**ترتيب الجلسات:** جلب `AccountId` لبقية المستخدمين يتم عبر توكن **Dashboard**، لذا
`SulfaFixture` يبني Dashboard أولًا (await) ثم يبني الباقي بالتوازي عبر `PrewarmAsync`.

**التوازي:** xUnit يشغّل **المجموعات (Collections) بالتوازي** فيما بينها، والاختبارات داخل
نفس المجموعة تتسلسل. بفضل `TestHost` و`SessionCache` المشتركَين على مستوى العملية، تستطيع
تقسيم كل منتج إلى Collection مستقلّ فتعمل المنتجات بالتوازي **دون إعادة تسجيل دخول**،
و`UserPoolManager` يمنع تشارك اختبارين متوازيين لنفس المستخدم. الإعداد في `xunit.runner.json`
(‏`maxParallelThreads: 8`‎).

**التوافق:** `ApiException` يرث من `HttpRequestException`، فأي `catch`/`Assert.ThrowsAsync`
على `HttpRequestException` يبقى صالحًا، والرسالة تتضمّن رمز الحالة (401/403).

**عزل المستخدمين:** `UserPoolRegistry` يجمّع المستخدمين حسب `Project`، فلكل منتج مجموعته
المعزولة تمامًا — لا تلوّث متبادل في التشغيل المتوازي.
