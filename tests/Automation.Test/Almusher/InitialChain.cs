using Automation.Test.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Almusher
{
    public class IniatialChain :BaseAlmusherTest
    {
        public IniatialChain(ITestOutputHelper testOutputHelper, AlmuhserFixture fixture) : base(testOutputHelper, fixture) { }
        

            [Fact]
            public async Task CreatePaymentChain_ShouldSucceed()
            {
                Output.WriteLine("\n🔗 اختبار: إنشاء سلسلة دفع جديدة");

                // 1. تنفيذ إنشاء السلسلة
                var chainId = await Almusher.CreatePaymentChainAsync(Dashboard.UserKey);

                // 2. التحقق من النتيجة
                Output.WriteLine($"📋 Chain ID: {chainId}");

                Assert.NotEqual(Guid.Empty, chainId);
                Assert.NotNull(chainId);

                // 3. التحقق من التخزين في StateManager
                var savedChainId = Fixture.State.GetValue<Guid>($"chain_{Dashboard.UserKey}");
                Output.WriteLine($"📋 Saved Chain ID in State: {savedChainId}");

                Assert.Equal(chainId, savedChainId);

                PrintResult(nameof(CreatePaymentChain_ShouldSucceed), true);
            }

            // ================================================================
            // ✅ اختبار 2: إنشاء سلسلة دفع متعددة
            // ================================================================
            [Fact]
            public async Task CreateMultiplePaymentChains_ShouldSucceed()
            {
                Output.WriteLine("\n🔗 اختبار: إنشاء عدة سلاسل دفع");

                var chainIds = new List<Guid>();

                for (int i = 1; i <= 3; i++)
                {
                    var chainId = await Almusher.CreatePaymentChainAsync(Dashboard.UserKey);
                    chainIds.Add(chainId);

                    Output.WriteLine($"📋 Chain #{i} ID: {chainId}");

                    Assert.NotEqual(Guid.Empty, chainId);

                    // التحقق من أن كل Chain ID فريد
                    if (i > 1)
                    {
                        Assert.NotEqual(chainIds[i - 2], chainId);
                    }
                }

                Output.WriteLine($"📋 تم إنشاء {chainIds.Count} سلاسل دفع بنجاح");

                PrintResult(nameof(CreateMultiplePaymentChains_ShouldSucceed), true);
            }
         }
        }
    

