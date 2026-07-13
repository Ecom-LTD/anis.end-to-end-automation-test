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

            // ✅ جلب محفظة LYD
            var lydWalletId = AnisCardLydWalletId;
            Output.WriteLine($"📋 Anis Card LYD Wallet ID: {lydWalletId}");

            // ✅ جلب محفظة USD
            var usdWalletId = AnisCardUsdWalletId;
            Output.WriteLine($"📋 Anis Card USD Wallet ID: {usdWalletId}");

            // ✅ جلب Session كامل (يحتوي على جميع المحافظ)
            var anisCardSession = AnisCard;
            Output.WriteLine($"📋 Anis Card Session Wallet ID: {anisCardSession.WalletId}");

            // ✅ التحقق
            Assert.False(string.IsNullOrEmpty(lydWalletId), "LYD Wallet ID should not be empty");
            Assert.False(string.IsNullOrEmpty(usdWalletId), "USD Wallet ID should not be empty");

            PrintResult(nameof(GetAnisCardWallets_ShouldSucceed), true);
        }

        [Fact]
        public async Task CheckAnisCardBalances_ShouldSucceed()
        {
            Output.WriteLine("\n💰 اختبار: جلب أرصدة محافظ Anis Card");

            // ✅ جلب رصيد LYD
            if (!string.IsNullOrEmpty(AnisCardLydWalletId))
            {
                var lydBalance = await Wallet.GetBalanceAsync(
                    AnisCardLyd.UserKey,
                    AnisCardLyd.WalletIdGuid);

                Output.WriteLine($"💰 LYD Balance: {lydBalance}");
            }

            // ✅ جلب رصيد USD
            if (!string.IsNullOrEmpty(AnisCardUsdWalletId))
            {
                var usdBalance = await Wallet.GetBalanceAsync(
                    AnisCardUsd.UserKey,
                    AnisCardUsd.WalletIdGuid);

                Output.WriteLine($"💰 USD Balance: {usdBalance}");
            }

            PrintResult(nameof(CheckAnisCardBalances_ShouldSucceed), true);
        }
    }
}
