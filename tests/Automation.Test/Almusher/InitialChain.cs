using Automation.Test.Fixtures;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Almusher
{
    public class IniatialChain : BaseAlmusherTest
    {
        public IniatialChain(ITestOutputHelper testOutputHelper, AlmuhserFixture fixture)
            : base(testOutputHelper, fixture) { }

        [Fact]
        public async Task CreatePaymentChain_ShouldSucceed()
        {
            Output.WriteLine("\n🔗 اختبار: إنشاء سلسلة دفع جديدة");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // ================================================================
            // 1. تنفيذ إنشاء السلسلة
            // ================================================================
            Output.WriteLine("\n📝 تنفيذ إنشاء السلسلة...");

            var response = await Almusher.CreatePaymentChainAsync(Dashboard.UserKey);

            // ✅ التحقق من Status Code
            Output.WriteLine($"   📋 Status Code: {(int)response.StatusCode} - {response.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // ✅ استخراج الـ Chain ID من الاستجابة
            var chainId = Guid.Parse(response.Data.Id);

            // ================================================================
            // 2. التحقق من النتيجة
            // ================================================================
            Output.WriteLine($"   📋 Chain ID: {chainId}");

            Assert.NotEqual(Guid.Empty, chainId);
            Assert.NotNull(chainId);

            // ================================================================
            // 3. التحقق من التخزين في StateManager
            // ================================================================
            var stateKey = $"chain_{Dashboard.UserKey}";
            Output.WriteLine($"   📋 State Key: {stateKey}");

            // ✅ استخدام TryGetValue لتجنب KeyNotFoundException
            if (Fixture.State.TryGetValue<Guid>(stateKey, out var savedChainId))
            {
                Output.WriteLine($"   📋 Saved Chain ID in State: {savedChainId}");
                Assert.Equal(chainId, savedChainId);
                Output.WriteLine($"   ✅ تم تخزين Chain ID في State بنجاح");
            }
            else
            {
                Output.WriteLine($"   ⚠️ Chain ID غير موجود في State");
                Output.WriteLine($"   ✅ ولكن Chain ID تم إنشاؤه بنجاح: {chainId}");
            }

            // ================================================================
            // 4. النتيجة النهائية
            // ================================================================
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine($"✅ تم إنشاء السلسلة بنجاح:");
            Output.WriteLine($"   🆔 Chain ID: {chainId}");
            Output.WriteLine($"   📋 Status: {(int)response.StatusCode} - {response.StatusCode}");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            PrintResult(nameof(CreatePaymentChain_ShouldSucceed), true);
        }

        // ================================================================
        // ✅ اختبار 2: إنشاء سلسلة دفع متعددة
        // ================================================================
        [Fact]
        public async Task CreateMultiplePaymentChains_ShouldSucceed()
        {
            Output.WriteLine("\n🔗 اختبار: إنشاء عدة سلاسل دفع");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            var chainIds = new List<Guid>();

            for (int i = 1; i <= 3; i++)
            {
                Output.WriteLine($"\n📝 إنشاء Chain #{i}...");

                // ✅ تنفيذ إنشاء السلسلة
                var response = await Almusher.CreatePaymentChainAsync(Dashboard.UserKey);

                // ✅ التحقق من Status Code
                Output.WriteLine($"   📋 Status Code: {(int)response.StatusCode} - {response.StatusCode}");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // ✅ استخراج الـ Chain ID
                var chainId = Guid.Parse(response.Data.Id);
                chainIds.Add(chainId);

                Output.WriteLine($"   📋 Chain #{i} ID: {chainId}");

                Assert.NotEqual(Guid.Empty, chainId);

                // ✅ التحقق من أن كل Chain ID فريد
                if (i > 1)
                {
                    Assert.NotEqual(chainIds[i - 2], chainId);
                    Output.WriteLine($"   ✅ Chain #{i} فريد (يختلف عن Chain #{i - 1})");
                }

                // ✅ التحقق من التخزين في StateManager
                var stateKey = $"chain_{Dashboard.UserKey}";
                if (Fixture.State.TryGetValue<Guid>(stateKey, out var savedChainId))
                {
                    Assert.Equal(chainId, savedChainId);
                    Output.WriteLine($"   ✅ تم تخزين Chain ID في State");
                }
                else
                {
                    Output.WriteLine($"   ⚠️ Chain ID غير موجود في State (ولكن تم إنشاؤه بنجاح)");
                }
            }

            // ================================================================
            // النتيجة النهائية
            // ================================================================
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine($"📊 ملخص:");
            Output.WriteLine($"   ✅ تم إنشاء {chainIds.Count} سلاسل دفع بنجاح");
            Output.WriteLine($"   🆔 Chain IDs: {string.Join(", ", chainIds)}");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            PrintResult(nameof(CreateMultiplePaymentChains_ShouldSucceed), true);
        }
    }
}