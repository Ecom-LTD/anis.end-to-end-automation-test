

       using Automation.Test.Almusher;
using Automation.Test.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Almusher
    {

        public class ExchangeDetailsDisplayTests : BaseAlmusherTest
        {
            public ExchangeDetailsDisplayTests(ITestOutputHelper output, AlmuhserFixture fixture)
                : base(output, fixture) { }

            [Fact]
            public async Task DisplayExchangeDetails()
            {
                Output.WriteLine("\n📊 عرض تفاصيل صرف العملات");
                Output.WriteLine("═══════════════════════════════════════════════════════════");

                // ✅ أدخل البيانات يدوياً من Postman
                var chainId = "5710b06b-eaa9-46c4-a04c-754837930361";
                var operationId = "8210c350-7832-4b76-90c1-34f3654730c1";

                Output.WriteLine($"\n📋 Chain ID: {chainId}");
                Output.WriteLine($"📋 Operation ID: {operationId}");

                // ✅ جلب التفاصيل
                var response = await Almusher.GetExchangeDetailsAsync(
                    Dashboard.UserKey,
                    chainId,
                    operationId);

                var data = response.Data;

                // ✅ عرض البيانات
                Output.WriteLine("\n📊 Exchange Details:");
                Output.WriteLine($"   🆔 ID:                 {data.Id}");
                Output.WriteLine($"   📈 Conversion Rate:    {data.ConversionRate:F10}");
                Output.WriteLine($"   💵 Buy Amount:         {data.CurrencyExchangeBuy?.Amount:F10}");
                Output.WriteLine($"   💰 Sell Amount:        {data.CurrencyExchangeSell?.Amount:F10}");
                Output.WriteLine($"   📝 Statement:          {data.DetailedStatement}");
                Output.WriteLine($"   💰 Lyd Rate:           {data.LydRate:F10}");

                // ✅ عرض JSON
                Output.WriteLine("\n📋 JSON:");
                Output.WriteLine(response.RawBody);

                PrintResult(nameof(DisplayExchangeDetails), true);
            }
        }
    }
