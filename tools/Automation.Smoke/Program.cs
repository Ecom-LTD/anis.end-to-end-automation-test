using Automation.Framework.Builders;
using Automation.Framework.Composition;
using Automation.Framework.Constants;
using Automation.Framework.Core.Session;
using Automation.Framework.Core.UserPool;
using Automation.Framework.Enums;
using Automation.Framework.Flows;
using Automation.Framework.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ===== تهيئة الحاوية (نفس ما يفعله TestHost في مشروع الاختبار) =====
var provider = new ServiceCollection().AddAutomationFramework().BuildServiceProvider();

var cache = provider.GetRequiredService<SessionCache>();
var pools = provider.GetRequiredService<UserPoolRegistry>();
var resilience = provider.GetRequiredService<ResilientSession>();
var wallet = provider.GetRequiredService<WalletFlow>();
var transfer = provider.GetRequiredService<TransferFlow>();

void Line(string s) => Console.WriteLine(s);
int passed = 0, failed = 0;
void Check(string name, bool ok) { if (ok) { passed++; Line($"  ✅ {name}"); } else { failed++; Line($"  ❌ {name}"); } }

SessionOptions Opt(string userKey, SubscriptionType type) => new()
{
    UserKey = userKey, LoadAccount = true, LoadWallet = true,
    CurrencyType = CurrencyType.LYD, RegionName = "Tripoli", HolderName = "Cash",
    SubscriptionType = type, SubscriptionName = UserHelper.GetUser(userKey).SubscriptionName
};

Line("═══════════════════════════════════════════════");
Line(" Smoke run: DI + SessionCache + UserPool + Scenario");
Line("═══════════════════════════════════════════════\n");

// 1) بناء Dashboard أولًا (مطلوب لجلب AccountId)
Line("[1] بناء جلسة Dashboard أولًا");
var dashboard = await cache.GetOrBuildAsync(Opt(TestUsers.Dashboard, SubscriptionType.Operator));
Check("Dashboard authenticated", dashboard.IsAuthenticated);
Check("Dashboard has AccountId", !string.IsNullOrEmpty(dashboard.AccountId));

// 2) Pre-warming لباقي جلسات Sulfa بالتوازي
Line("\n[2] Pre-warming لجلسات Sulfa بالتوازي");
var operatorOpt = Opt(TestUsers.SulfaOperator, SubscriptionType.Operator);
var businessOpt = Opt(TestUsers.SulfaBusiness, SubscriptionType.Business);
await cache.PrewarmAsync(new[] { operatorOpt, businessOpt });
var op = await cache.GetOrBuildAsync(operatorOpt);
var biz = await cache.GetOrBuildAsync(businessOpt);
Check("Operator has Wallet", op.HasWallet);
Check("Business has Subscription", !string.IsNullOrEmpty(biz.SubscriptionId));

// 3) إعادة استخدام الكاش (نفس الكائن، بدون إعادة تسجيل دخول)
Line("\n[3] التحقق من إعادة استخدام الكاش");
var opAgain = await cache.GetOrBuildAsync(operatorOpt);
Check("Cache returns same session instance", ReferenceEquals(op, opAgain));

// 4) مجموعة المستخدمين: حجز/تحرير
Line("\n[4] مجموعة المستخدمين (Reserve/Release) لمشروع Sulfa");
var sulfaPool = pools.For("Sulfa");
int availableBefore = sulfaPool.Available("Operator");
await using (var lease = await sulfaPool.ReserveAsync("Operator"))
{
    Check("Reserved an operator", !string.IsNullOrEmpty(lease.UserKey));
    Check("Availability decreased while reserved", sulfaPool.Available("Operator") == availableBefore - 1);
}
Check("Availability restored after release", sulfaPool.Available("Operator") == availableBefore);

// 5) عزل المشاريع
Line("\n[5] عزل المشاريع");
Check("Forex pool exists & isolated", pools.For("Forex").Total("Trader") >= 1);

// 6) سيناريو متعدّد الخدمات: رصيد ← تحويل ← تحقق
Line("\n[6] سيناريو: balance → transfer → verify (Wallet + Transfer)");
const decimal amount = 5m;
var before = await resilience.ExecuteAsync(op, () => wallet.GetBalanceAsync(op.UserKey, op.WalletIdGuid));
Line($"     رصيد المرسل قبل: {before}");
var result = await transfer.TransferAsync(op.UserKey, op.WalletId, biz.SubscriptionId, amount, op.RegionId);
Check("Transfer succeeded", result.Success);
var after = await resilience.ExecuteAsync(op, () => wallet.GetBalanceAsync(op.UserKey, op.WalletIdGuid));
Line($"     رصيد المرسل بعد:  {after}");
Check("Sender balance decreased by amount", after == before - amount);

Line("\n═══════════════════════════════════════════════");
Line($" النتيجة: {passed} نجاح / {failed} فشل");
Line("═══════════════════════════════════════════════");
return failed == 0 ? 0 : 1;
