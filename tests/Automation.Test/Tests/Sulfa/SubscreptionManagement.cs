using Automation.Test.Fixtures.Fazza;
using Automation.Test.Tests.Sulfa.Base;

using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Sulfa
{
    public class SubscreptionManagement :BaseSulfaTest 
    {
        public SubscreptionManagement(ITestOutputHelper testOutputHelper, SulfaFixture Fixture) : base(testOutputHelper, Fixture)
        { }
        
            [Fact]
            public async Task GetCashflowReportV2_ShouldSucceed()
            {
                Output.WriteLine("\n💰 اختبار: جلب تقرير التدفق النقدي V2");

                // تنفيذ الطلب
                var response = await CashFlow.GetCashflowReportAsync(
                    SulfaOperator.UserKey,
                    SulfaBusiness.PhoneNumber);

                // التحقق
                Assert.NotNull(response);
                Assert.True(response.Results.Count > 0, "لا توجد نتائج للتقرير");

                // عرض أول نتيجة
                var firstItem = response.Results.First();
                Output.WriteLine($"📋 التقرير الأول:");
                Output.WriteLine($"   📛 اسم المشترك: {firstItem.SubscriptionName}");
                Output.WriteLine($"   📞 رقم الهاتف: {firstItem.PhoneNumber}");
                Output.WriteLine($"   💰 الرصيد الحالي: {firstItem.Balance}");
                Output.WriteLine($"   💳 إجمالي الرصيد: {firstItem.TotalBalance}");
                Output.WriteLine($"   💰 الفزعة الحالية: {firstItem.CurrentFazaa}");
                Output.WriteLine($"   💰 السلفة الحالية: {firstItem.CurrentSulfa}");
                Output.WriteLine($"   💳 الفزعة المستحقة: {firstItem.CurrentFazaaDue}");
                Output.WriteLine($"   💳 السلفة المستحقة: {firstItem.CurrentSulfaDue}");
                Output.WriteLine($"📊  إجمالي المستحق: {firstItem.TotalDue}");
                Output.WriteLine($"   📊 إجمالي الغير مستحق: {firstItem.TotalNotDue}");
                Output.WriteLine($"   📊 الاجل  المغطى: {firstItem.IsDelayedCovered}");
                Output.WriteLine($"   📊 السلفة المغطاة: {firstItem.IsSulfaCovered}");

            PrintResult(nameof(GetCashflowReportV2_ShouldSucceed), true);
            }
        }
    }
