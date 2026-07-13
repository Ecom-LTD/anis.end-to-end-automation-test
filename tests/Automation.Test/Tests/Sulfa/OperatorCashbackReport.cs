
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Tests.Sulfa.Base;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Sulfa
{
    public class OperatorCashbackReport : BaseSulfaTest
    {
        public OperatorCashbackReport(ITestOutputHelper output, SulfaFixture fixture) : base(output, fixture) { }


        // ================================================================
        // ✅ اختبار 1: جلب التقرير الأسبوعي لمشغل معين
        // ================================================================
        [Fact]
        public async Task GetWeeklyReport_ForOperator_ShouldSucceed()
        {
            Output.WriteLine("\n📊 اختبار: جلب تقرير الكاش باك الأسبوعي للمشغل");

            // 1. تحديد التواريخ (آخر 7 أيام)
            var toDate = DateTime.Now;
            var fromDate = toDate.AddDays(-7);

            // 2. تنفيذ الطلب
            var response = await OperatorCashback.GetWeeklyReportAsync(
                userKey: Dashboard.UserKey,      // المستخدم الذي يقوم بالطلب
                phone: "0915751122",    // رقم هاتف المشغل
                fromDate: fromDate,
                toDate: toDate,
                currentPage: 1,
                pageSize: 25);

            // 3. التحقق من النتائج
            Assert.NotNull(response);
            Assert.NotNull(response.Data);
            Assert.Equal("success", response.Message);

            // 4. عرض النتائج
            Output.WriteLine($"📋 Total Results: {response.Data?.Results?.Count ?? 0}");
            Output.WriteLine($"📋 Current Page: {response.Data?.CurrentPage}");
            Output.WriteLine($"📋 Total Pages: {response.Data?.LastPage}");

            if (response.Data?.Results != null && response.Data.Results.Count > 0)
            {
                var firstItem = response.Data.Results.First();
                Output.WriteLine($"\n📈 أول نتيجة:");
                Output.WriteLine($"   📋 Subscription: {firstItem.SubscriptionName}");
                Output.WriteLine($"   📞 Phone: {firstItem.Phone}");
                Output.WriteLine($"   💰 Cashback: {firstItem.CashbackValue:N2}");
                Output.WriteLine($"   💳 Debit: {firstItem.DebitValue:N2}");
                Output.WriteLine($"   🛒 Purchase: {firstItem.PurchaseValue:N2}");
                Output.WriteLine($"   💳 TotalDebit: {firstItem.TotalDebit:N2}");
                Output.WriteLine($"   🛒 TotalPurchase: {firstItem.TotalPurchase:N2}");
                Output.WriteLine($"   📅 From: {firstItem.FromDate:yyyy-MM-dd}");
                Output.WriteLine($"   📅 To: {firstItem.ToDate:yyyy-MM-dd}");


            }

            PrintResult(nameof(GetWeeklyReport_ForOperator_ShouldSucceed), true);
        }



        // ================================================================
        // ✅ اختبار 1: جلب التقرير الشهري  لمشغل معين
        // ================================================================
        // ================================================================
        [Fact]
        public async Task GetMonthlyReport_ForOperator_ShouldSucceed()
        {
            Output.WriteLine("\n📊 اختبار: جلب تقرير الشهري للمشغل");

            // 1. تحديد التواريخ (آخر 30 يوم)
            var toDate = DateTime.Now;
            var fromDate = toDate.AddDays(-30);

            // 2. تنفيذ الطلب
            var response = await OperatorCashback.GetMonthlyReportAsync(
                userKey: Dashboard.UserKey,      // المستخدم الذي يقوم بالطلب
                phone: "0915751122",             // رقم هاتف المشغل
                fromDate: fromDate,
                toDate: toDate,
                currentPage: 1,
                pageSize: 25);

            // 3. التحقق من النتائج
            Assert.NotNull(response);
            Assert.NotNull(response.Data);
            Assert.Equal("success", response.Message);

            // 4. عرض النتائج
            Output.WriteLine($"\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("📋 معلومات التقرير:");
            Output.WriteLine($"   📊 Total Results: {response.Data?.Results?.Count ?? 0}");
            Output.WriteLine($"   📄 Current Page: {response.Data?.CurrentPage}");
            Output.WriteLine($"   📄 Total Pages: {response.Data?.LastPage}");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            if (response.Data?.Results != null && response.Data.Results.Count > 0)
            {
                var firstItem = response.Data.Results.First();

                Output.WriteLine("\n📈 تفاصيل التقرير الشهري:");
                Output.WriteLine("─────────────────────────────────────────────────────────────");

                // ============================================================
                // ✅ عرض جميع خصائص MonthlyOperatorCashbackReportItem
                // ============================================================

                // 📋 معلومات أساسية
                Output.WriteLine("📋 المعلومات الأساسية:");
                Output.WriteLine($"   🆔 ID: {firstItem.Id}");
                Output.WriteLine($"   📛 Subscription Name: {firstItem.SubscriptionName}");
                Output.WriteLine($"   📞 Phone: {firstItem.Phone}");
                Output.WriteLine($"   📍 Location: {firstItem.Location}");
                Output.WriteLine($"   📅 Date: {firstItem.Date:yyyy-MM-dd}");
                Output.WriteLine("─────────────────────────────────────────────────────────────");

                // 💰 القيم المالية الأساسية
                Output.WriteLine("💰 القيم المالية الأساسية:");
                Output.WriteLine($"   💳 Total Debit: {firstItem.TotalDebit:N2}");
                Output.WriteLine($"   🛒 Total Purchase: {firstItem.TotalPurchase:N2}");
                Output.WriteLine($"   💳 Total Holder Debit: {firstItem.TotalHolderDebit:N2}");
                Output.WriteLine($"   🛒 Total Holder Purchase: {firstItem.TotalHolderPurchase:N2}");
                Output.WriteLine("─────────────────────────────────────────────────────────────");

                // 📊 نسب الشراء
                Output.WriteLine("📊 نسب الشراء (Purchase Ratios):");
                Output.WriteLine($"   📈 Total Purchase Topup Ratio: {firstItem.TotalPurchaseTopupRatio:N2}%");
                Output.WriteLine($"   📈 Total Purchase Delayed Topup Ratio: {firstItem.TotalPurchaseDelayedTopupRatio:N2}%");
                Output.WriteLine($"   📈 Total Purchase Urgent Topup Ratio: {firstItem.TotalPurchaseUrgentTopupRatio:N2}%");
                Output.WriteLine("─────────────────────────────────────────────────────────────");

                // 🏦 نسب السلفة (Sulfa)
                Output.WriteLine("🏦 نسب السلفة (Sulfa Ratios):");
                Output.WriteLine($"   📈 Sulfa (Default Overdue): {firstItem.TotalPurchaseSulfaTopupWithinDefaultOverdueTimeRatio:N2}%");
                Output.WriteLine($"   📈 Sulfa (Extended Overdue): {firstItem.TotalPurchaseSulfaTopupWithinExtendedOverdueTimeRatio:N2}%");
                Output.WriteLine($"   📈 Sulfa (Outside Overdue): {firstItem.TotalPurchaseSulfaTopupOutsideOverdueTimeRatio:N2}%");
                Output.WriteLine("─────────────────────────────────────────────────────────────");

                // 💳 نسب الفزعة (Fazaa)
                Output.WriteLine("💳 نسب الفزعة (Fazaa Ratios):");
                Output.WriteLine($"   📈 Fazaa Personal Transfer: {firstItem.TotalPurchaseFazaaTopUpPersonalTransferRatio:N2}%");
                Output.WriteLine($"   📈 Fazaa Mediator Payment: {firstItem.TotalPurchaseFazaaTopUpMediatorPaymentRatio:N2}%");
                Output.WriteLine($"   📈 Fazaa External Transfer: {firstItem.TotalPurchaseFazaaTopUpExternalTransferRatio:N2}%");
                Output.WriteLine("─────────────────────────────────────────────────────────────");

                // 📊 إجماليات (إذا كانت متوفرة)
                Output.WriteLine("\n📊 إجماليات التقرير:");
                var totalDebit = response.Data.Results.Sum(x => x.TotalDebit);
                var totalPurchase = response.Data.Results.Sum(x => x.TotalPurchase);
                var totalHolderDebit = response.Data.Results.Sum(x => x.TotalHolderDebit);
                var totalHolderPurchase = response.Data.Results.Sum(x => x.TotalHolderPurchase);

                Output.WriteLine($"   💳 إجمالي Total Debit: {totalDebit:N2}");
                Output.WriteLine($"   🛒 إجمالي Total Purchase: {totalPurchase:N2}");
                Output.WriteLine($"   💳 إجمالي Total Holder Debit: {totalHolderDebit:N2}");
                Output.WriteLine($"   🛒 إجمالي Total Holder Purchase: {totalHolderPurchase:N2}");
                Output.WriteLine("═══════════════════════════════════════════════════════════");
            }
            else
            {
                Output.WriteLine("⚠️ لا توجد نتائج في التقرير");
            }

            PrintResult(nameof(GetMonthlyReport_ForOperator_ShouldSucceed), true);
        }
    }
}
