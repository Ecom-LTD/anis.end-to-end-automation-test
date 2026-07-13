using Automation.Framework.Core.Http;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Tests.Sulfa.Base;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Sulfa.SulfaRequest
{
    public class SulfaRequest : BaseSulfaTest
    {
        public SulfaRequest(ITestOutputHelper output, SulfaFixture fixture)
            : base(output, fixture) { }

        [Fact]
        public async Task RequestSulfa_WithValidAmount_ShouldSucceed()
        {
            Output.WriteLine("\n✅ اختبار: طلب سلفة بقيمة صالحة");

            var amount = 1000m;

            var result = await ExecuteWithRetryAsync(SulfaOperator, async () =>
                await FazzaTopUp.RequestSulfaAsync(
                    SulfaOperator.UserKey,
                    amount,
                    SulfaBusiness.SubscriptionId,  // recipient
                    SulfaOperator.WalletId));

            PrintResult(nameof(RequestSulfa_WithValidAmount_ShouldSucceed), !string.IsNullOrEmpty(result));
        }

        [Fact]
        public async Task RequestSulfa_WithZeroAmount_ShouldFail()
        {
            Output.WriteLine("\n❌ اختبار: طلب سلفة بقيمة 0");

            // ✅ استخدام ApiException بدلاً من HttpRequestException
            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                FazzaTopUp.RequestSulfaAsync(
                    SulfaOperator.UserKey,
                    0,  // ✅ قيمة 0 (يجب أن ترفض)
                    SulfaBusiness.SubscriptionId,
                    SulfaOperator.WalletId));

            // ✅ التحقق من تفاصيل الخطأ
            Output.WriteLine($"📋 Status Code: {exception.ApiStatusCode}");
            Output.WriteLine($"📋 Body: {exception.Body}");

            Assert.Equal(HttpStatusCode.BadRequest, exception.ApiStatusCode);
            Assert.Contains("Fazaa top up amount must be greater than zero", exception.Body);

            PrintResult(nameof(RequestSulfa_WithZeroAmount_ShouldFail), true);
        }

        [Fact]
        public async Task RequestSulfa_WithNegativeAmount_ShouldFail()
        {
            Output.WriteLine("\n❌ اختبار: طلب سلفة بقيمة سالبة");

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                FazzaTopUp.RequestSulfaAsync(
                    SulfaOperator.UserKey,
                    -100m,
                    SulfaBusiness.SubscriptionId,
                    SulfaOperator.WalletId));

            Output.WriteLine($"📋 Status Code: {exception.ApiStatusCode}");
            Output.WriteLine($"📋 Body: {exception.Body}");

            Assert.Equal(HttpStatusCode.BadRequest, exception.ApiStatusCode);
            Assert.Contains("Fazaa top up amount must be greater than zero", exception.Body);

            PrintResult(nameof(RequestSulfa_WithNegativeAmount_ShouldFail), true);
        }
    }
}
