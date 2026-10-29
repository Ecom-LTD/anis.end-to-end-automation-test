using Automation.Test.Fixtures.Fazza;
using Automation.Test.Tests.Sulfa.Base;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Test.Infrastructure.Reporting;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Sulfa
{
    [Trait("Category", "Smoke")]
    public class WalletTests : BaseSulfaTest
    {
        public WalletTests(ITestOutputHelper output, SulfaFixture fixture) : base(output, fixture) { }



        [Fact]
        public void Sessions_ShouldBePrewarmed()
        {
            Assert.True(Dashboard.IsAuthenticated, "Dashboard should be authenticated");
            Assert.True(SulfaOperator.IsAuthenticated, "SulfaOperator should be authenticated");
            Assert.True(SulfaBusiness.IsAuthenticated, "SulfaBusiness should be authenticated");
            PrintResult(nameof(Sessions_ShouldBePrewarmed), true);
        }

        [Fact]
        public async Task UpdateDefaultWallet_ShouldSucceed()
        {
            // Arrange
            Output.WriteLine("\n💰 اختبار: تحديث المحفظة الافتراضية");

            var userKey = SulfaBusiness.UserKey;
            var walletId = SulfaBusiness.WalletId;  // المحفظة الجديدة

            Output.WriteLine($"📋 المستخدم: {userKey}");
            Output.WriteLine($"📋 المحفظة الجديدة: {walletId}");

            // Act
            var result = await Wallet.UpdateDefaultWalletAsync(userKey, walletId);

            // Assert
            Output.WriteLine($"📋 النتيجة: {result}");
            Assert.Equal("Wallet updated successfully", result);

            PrintResult(nameof(UpdateDefaultWallet_ShouldSucceed), true);
        }

 

        [Fact]
        public async Task GetBalance_ForOperator_ShouldSucceed()
        {
            // رصيد Operator/Business يمكن أن يكون سالبًا في النظام، فلا نفترض >= 0؛
            // المطلوب أن تُقرأ المحفظة بنجاح.
            Output.Scenario("قراءة رصيد محفظة Operator", "قراءة الرصيد بنجاح (قد يكون سالبًا)");
            Output.Inputs(("المستخدم", SulfaOperator.UserKey), ("المحفظة", SulfaOperator.WalletId));

            var balance = await ExecuteWithRetryAsync(SulfaOperator,
                () => Wallet.GetBalanceAsync(SulfaOperator.UserKey, SulfaOperator.WalletIdGuid));

            Output.After(("الرصيد", balance));
            PrintResult(nameof(GetBalance_ForOperator_ShouldSucceed), true);
        }
    }
}
