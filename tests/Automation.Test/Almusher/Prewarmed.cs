using Automation.Framework.Core.Enums;
using Automation.Test.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Almusher
{
    public class Prewarmed : BaseAlmusherTest
    {
        public Prewarmed(ITestOutputHelper output, AlmuhserFixture fixture) : base(output, fixture) { }



        [Fact]
        public void Sessions_ShouldBePrewarmed()
        {
            Assert.True(Dashboard.IsAuthenticated, "Dashboard should be authenticated");
            Assert.True(AnisCard.IsAuthenticated, "AnisCard should be authenticated");
            Assert.True(AnisPay.IsAuthenticated, "AnisPay should be authenticated");
            Assert.True(Hreysh.IsAuthenticated, "Hreysh should be authenticated");
            Assert.True(Profit.IsAuthenticated, "Profit should be authenticated");
            Assert.True(Commission.IsAuthenticated, "Commission should be authenticated");
            PrintResult(nameof(Sessions_ShouldBePrewarmed), true);
        }
        [Fact]
        public async Task GetAnisCardWallets_ShouldSucceed()
        {
            Output.WriteLine("\n💰 اختبار: جلب محافظ Anis Card");

            // ================================================================
            // ✅ جلب محفظة LYD باستخدام WalletFlow
            // ================================================================
            var lydWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisCard.UserKey,           // المستخدم
                CurrencyType.LYD,           // العملة
                "Tripoli",                  // المنطقة
                "Cash");                    // اسم الحامل

            Output.WriteLine($"📋 Anis Card LYD Wallet ID: {lydWalletId}");

            // ================================================================
            // ✅ جلب محفظة USD باستخدام WalletFlow
            // ================================================================
            var usdWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisCard.UserKey,
                CurrencyType.USD,
                "Tripoli",
                "Cash");

            Output.WriteLine($"📋 Anis Card USD Wallet ID: {usdWalletId}");

            // ================================================================
            // ✅ جلب Session Wallet ID (المحفظة الافتراضية)
            // ================================================================
            var anisCardSessionWalletId = AnisCard.WalletId;
            Output.WriteLine($"📋 Anis Card Session Wallet ID: {anisCardSessionWalletId}");

            // ================================================================
            // ✅ جلب جميع المحافظ دفعة واحدة (اختياري)
            // ================================================================
            var allWallets = await Wallet.GetAllWalletsAsync(
                AnisCard.UserKey,
                "Tripoli",
                "Cash");

            Output.WriteLine($"\n📋 جميع محافظ Anis Card:");
            foreach (var (currency, walletId) in allWallets)
            {
                Output.WriteLine($"   💰 {currency}: {walletId}");
            }

            // ================================================================
            
        }

        [Fact]
        public async Task CheckAnisCardBalances_ShouldSucceed()
        {
            Output.WriteLine("\n💰 اختبار: جلب أرصدة محافظ Anis Card");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // ================================================================
            // 1. جلب المحافظ
            // ================================================================
            Output.WriteLine("\n📝 جلب المحافظ...");

            var lydWalletId = await Wallet.GetOrLoadWalletIdAsync(AnisCardLyd.UserKey, CurrencyType.LYD);
            var usdWalletId = await Wallet.GetOrLoadWalletIdAsync(AnisCardUsd.UserKey, CurrencyType.USD);

            Output.WriteLine($"   📋 LYD Wallet ID: {lydWalletId}");
            Output.WriteLine($"   📋 USD Wallet ID: {usdWalletId}");

            // ================================================================
            // 2. جلب الأرصدة الحالية (قبل التحديث)
            // ================================================================
            Output.WriteLine("\n💰 جلب الأرصدة الحالية...");

            var lydBalanceBefore = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, lydWalletId);
            var usdBalanceBefore = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, usdWalletId);

            Output.WriteLine($"   💰 LYD Balance (before): {lydBalanceBefore:F4}");
            Output.WriteLine($"   💰 USD Balance (before): {usdBalanceBefore:F4}");

            // ================================================================
            // 3. تحديث المحفظة الافتراضية إلى LYD وجلب الرصيد
            // ================================================================
            Output.WriteLine("\n📝 تحديث المحفظة الافتراضية إلى LYD...");

            await Wallet.UpdateDefaultWalletAsync(AnisCardLyd.UserKey, lydWalletId.ToString());
            Output.WriteLine("   ✅ Updated to LYD");

            var lydBalanceAfter = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, lydWalletId);
            Output.WriteLine($"   💰 LYD Balance (after): {lydBalanceAfter:F4}");

            // ================================================================
            // 4. تحديث المحفظة الافتراضية إلى USD وجلب الرصيد
            // ================================================================
            Output.WriteLine("\n📝 تحديث المحفظة الافتراضية إلى USD...");

            await Wallet.UpdateDefaultWalletAsync(AnisCardUsd.UserKey, usdWalletId.ToString());
            Output.WriteLine("   ✅ Updated to USD");

            var usdBalanceAfter = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, usdWalletId);
            Output.WriteLine($"   💰 USD Balance (after): {usdBalanceAfter:F4}");

            // ================================================================
            // 5. التحقق
            // ================================================================
            Output.WriteLine("\n✅ التحقق...");

            Assert.NotEqual(Guid.Empty, lydWalletId);
            Assert.NotEqual(Guid.Empty, usdWalletId);
            Assert.NotEqual(lydWalletId, usdWalletId);
            Assert.True(lydBalanceAfter <= 0);
            Assert.True(usdBalanceAfter >= 0);

            Output.WriteLine("   ✅ جميع التحققات نجحت");

            // ================================================================
            // 6. النتيجة النهائية
            // ================================================================
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine($"📊 ملخص الأرصدة:");
            Output.WriteLine($"   💰 LYD: {lydBalanceAfter:F4} (Wallet: {lydWalletId})");
            Output.WriteLine($"   💰 USD: {usdBalanceAfter:F4} (Wallet: {usdWalletId})");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            PrintResult(nameof(CheckAnisCardBalances_ShouldSucceed), true);
        }

        [Fact]
        public async Task GetAllAlmusheerWallets_ShouldSucceed()
        {
            Output.WriteLine("\n💰 اختبار: جلب جميع محافظ AlMusheer");

            // ================================================================
            // ✅ 1. جلب جميع المحافظ المطلوبة
            // ================================================================

            // ✅ anispay → LYD
            var anisPayLydWalletId = AnisPay?.WalletId ?? string.Empty;
            Output.WriteLine($"📋 Anis Pay LYD Wallet ID: {anisPayLydWalletId}");

            // ✅ aniscard → LYD
            var anisCardLydWalletId = AnisCardLydWalletId;
            Output.WriteLine($"📋 Anis Card LYD Wallet ID: {anisCardLydWalletId}");

            // ✅ aniscard → USD
            var anisCardUsdWalletId = AnisCardUsdWalletId;
            Output.WriteLine($"📋 Anis Card USD Wallet ID: {anisCardUsdWalletId}");

            // ✅ hreysh → USD
            var hreyshUsdWalletId = Hreysh?.WalletId ?? string.Empty;
            Output.WriteLine($"📋 Hreysh USD Wallet ID: {hreyshUsdWalletId}");

            // ✅ profit → LYD
            var profitLydWalletId = Profit?.WalletId ?? string.Empty;
            Output.WriteLine($"📋 Profit LYD Wallet ID: {profitLydWalletId}");

            // ✅ commission → USD
            var commissionUsdWalletId = Commission?.WalletId ?? string.Empty;
            Output.WriteLine($"📋 Commission USD Wallet ID: {commissionUsdWalletId}");

            // ================================================================
            // ✅ 2. عرض الأرصدة (اختياري)
            // ================================================================

            Output.WriteLine("\n💰 أرصدة المحافظ:");

            if (!string.IsNullOrEmpty(anisPayLydWalletId))
            {
                var balance = await Wallet.GetBalanceAsync(AnisPay.UserKey, AnisPay.WalletIdGuid);
                Output.WriteLine($"   Anis Pay LYD: {balance:F4}");
            }

            if (!string.IsNullOrEmpty(anisCardLydWalletId))
            {
                var balance = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, AnisCardLyd.WalletIdGuid);
                Output.WriteLine($"   Anis Card LYD: {balance:F4}");
            }

            if (!string.IsNullOrEmpty(anisCardUsdWalletId))
            {
                var balance = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, AnisCardUsd.WalletIdGuid);
                Output.WriteLine($"   Anis Card USD: {balance:F4}");
            }

            if (!string.IsNullOrEmpty(hreyshUsdWalletId))
            {
                var balance = await Wallet.GetBalanceAsync(Hreysh.UserKey, Hreysh.WalletIdGuid);
                Output.WriteLine($"   Hreysh USD: {balance:F4}");
            }

            if (!string.IsNullOrEmpty(profitLydWalletId))
            {
                var balance = await Wallet.GetBalanceAsync(Profit.UserKey, Profit.WalletIdGuid);
                Output.WriteLine($"   Profit LYD: {balance:F4}");
            }

            if (!string.IsNullOrEmpty(commissionUsdWalletId))
            {
                var balance = await Wallet.GetBalanceAsync(Commission.UserKey, Commission.WalletIdGuid);
                Output.WriteLine($"   Commission USD: {balance:F4}");
            }

            // ================================================================
            // ✅ 3. عرض ملخص المحافظ
            // ================================================================

            Output.WriteLine("\n📊 ملخص محافظ AlMusheer:");
            Output.WriteLine("┌─────────────────────┬─────────────┬──────────────────────────────────────┐");
            Output.WriteLine("│ المستخدم            │ العملة      │ Wallet ID                            │");
            Output.WriteLine("├─────────────────────┼─────────────┼──────────────────────────────────────┤");
            Output.WriteLine($"│ Anis Pay            │ LYD         │ {anisPayLydWalletId,-36} │");
            Output.WriteLine($"│ Anis Card           │ LYD         │ {anisCardLydWalletId,-36} │");
            Output.WriteLine($"│ Anis Card           │ USD         │ {anisCardUsdWalletId,-36} │");
            Output.WriteLine($"│ Hreysh              │ USD         │ {hreyshUsdWalletId,-36} │");
            Output.WriteLine($"│ Profit              │ LYD         │ {profitLydWalletId,-36} │");
            Output.WriteLine($"│ Commission          │ USD         │ {commissionUsdWalletId,-36} │");
            Output.WriteLine("└─────────────────────┴─────────────┴──────────────────────────────────────┘");

            // ================================================================
            // ✅ 4. التحقق من وجود جميع المحافظ
            // ================================================================

            Assert.False(string.IsNullOrEmpty(anisPayLydWalletId), "Anis Pay LYD Wallet should not be empty");
            Assert.False(string.IsNullOrEmpty(anisCardLydWalletId), "Anis Card LYD Wallet should not be empty");
            Assert.False(string.IsNullOrEmpty(anisCardUsdWalletId), "Anis Card USD Wallet should not be empty");
            Assert.False(string.IsNullOrEmpty(hreyshUsdWalletId), "Hreysh USD Wallet should not be empty");
            Assert.False(string.IsNullOrEmpty(profitLydWalletId), "Profit LYD Wallet should not be empty");
            Assert.False(string.IsNullOrEmpty(commissionUsdWalletId), "Commission USD Wallet should not be empty");

            Output.WriteLine("\n✅ جميع محافظ AlMusheer تم جلبها بنجاح!");

            PrintResult(nameof(GetAllAlmusheerWallets_ShouldSucceed), true);
        }
    }
}
