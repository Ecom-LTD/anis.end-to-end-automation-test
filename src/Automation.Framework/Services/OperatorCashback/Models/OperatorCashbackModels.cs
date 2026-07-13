using Newtonsoft.Json;
using System;


namespace Automation.Framework.Services.OperatorCashback.Models
{

    /// <summary>
    /// طلب تقرير الكاش باك للمشغل
    /// </summary>
    public class OperatorCashbackReportRequest
    {
        [JsonProperty("LocationId")]
        public string? LocationId { get; set; } = "";

        [JsonProperty("FromDate")]
        public string FromDate { get; set; } = string.Empty;

        [JsonProperty("ToDate")]
        public string ToDate { get; set; } = string.Empty;

        [JsonProperty("CurrentPage")]
        public int CurrentPage { get; set; } = 1;

        [JsonProperty("PageSize")]
        public int PageSize { get; set; } = 25;

        [JsonProperty("Phone")]
        public string Phone { get; set; } = string.Empty;
    }

    /// <summary>
    /// البيانات الأساسية المشتركة بين جميع التقارير (الترحيل)
    /// </summary>
    public class BaseReportData
    {
        [JsonProperty("currentPage")]
        public int CurrentPage { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("showingFrom")]
        public int ShowingFrom { get; set; }

        [JsonProperty("showingTo")]
        public int ShowingTo { get; set; }

        [JsonProperty("lastPage")]
        public int LastPage { get; set; }

        [JsonProperty("pages")]
        public List<int> Pages { get; set; } = new();
    }

    // ================================================================
    // ✅ التقرير الأسبوعي (Weekly Report)
    // ================================================================

    /// <summary>
    /// عنصر تقرير الكاش باك الأسبوعي
    /// </summary>
    public class WeeklyOperatorCashbackReportItem
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("subscriptionName")]
        public string SubscriptionName { get; set; } = string.Empty;

        [JsonProperty("cashbackValue")]
        public decimal CashbackValue { get; set; }

        [JsonProperty("fromDate")]
        public DateTime FromDate { get; set; }

        [JsonProperty("toDate")]
        public DateTime ToDate { get; set; }

        [JsonProperty("debitValue")]
        public decimal DebitValue { get; set; }

        [JsonProperty("purchaseValue")]
        public decimal PurchaseValue { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; } = string.Empty;

        [JsonProperty("totalDebit")]
        public decimal TotalDebit { get; set; }

        [JsonProperty("totalPurchase")]
        public decimal TotalPurchase { get; set; }

        [JsonProperty("averageCashback")]
        public decimal AverageCashback { get; set; }

        [JsonProperty("averageCashbackRatio")]
        public decimal AverageCashbackRatio { get; set; }

        [JsonProperty("firstAverageValue")]
        public decimal FirstAverageValue { get; set; }

        [JsonProperty("secondAverageValue")]
        public decimal SecondAverageValue { get; set; }

        [JsonProperty("thirdAverageValue")]
        public decimal ThirdAverageValue { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; } = string.Empty;
    }

    /// <summary>
    /// بيانات التقرير الأسبوعي
    /// </summary>
    public class WeeklyOperatorCashbackReportData : BaseReportData
    {
        [JsonProperty("results")]
        public List<WeeklyOperatorCashbackReportItem> Results { get; set; } = new();
    }

    /// <summary>
    /// استجابة تقرير الكاش باك الأسبوعي
    /// </summary>
    public class WeeklyOperatorCashbackReportResponse
    {
        [JsonProperty("data")]
        public WeeklyOperatorCashbackReportData Data { get; set; } = new();

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }

    // ================================================================
    // ✅ التقرير الشهري (Monthly Report)
    // ================================================================

    /// <summary>
    /// عنصر تقرير الكاش باك الشهري
    /// </summary>
    public class MonthlyOperatorCashbackReportItem
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("subscriptionName")]
        public string SubscriptionName { get; set; } = string.Empty;

        [JsonProperty("location")]
        public string Location { get; set; } = string.Empty;

        [JsonProperty("totalPurchase")]
        public decimal TotalPurchase { get; set; }

        [JsonProperty("totalDebit")]
        public decimal TotalDebit { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonProperty("totalPurchaseTopupRatio")]
        public decimal TotalPurchaseTopupRatio { get; set; }

        [JsonProperty("totalHolderDebit")]
        public decimal TotalHolderDebit { get; set; }

        [JsonProperty("totalHolderPurchase")]
        public decimal TotalHolderPurchase { get; set; }

        [JsonProperty("totalPurchaseDelayedTopupRatio")]
        public decimal TotalPurchaseDelayedTopupRatio { get; set; }

        [JsonProperty("totalPurchaseUrgentTopupRatio")]
        public decimal TotalPurchaseUrgentTopupRatio { get; set; }

        [JsonProperty("totalPurchaseSulfaTopupWithinDefaultOverdueTimeRatio")]
        public decimal TotalPurchaseSulfaTopupWithinDefaultOverdueTimeRatio { get; set; }

        [JsonProperty("totalPurchaseSulfaTopupWithinExtendedOverdueTimeRatio")]
        public decimal TotalPurchaseSulfaTopupWithinExtendedOverdueTimeRatio { get; set; }

        [JsonProperty("totalPurchaseSulfaTopupOutsideOverdueTimeRatio")]
        public decimal TotalPurchaseSulfaTopupOutsideOverdueTimeRatio { get; set; }

        [JsonProperty("totalPurchaseFazaaTopUpPersonalTransferRatio")]
        public decimal TotalPurchaseFazaaTopUpPersonalTransferRatio { get; set; }

        [JsonProperty("totalPurchaseFazaaTopUpMediatorPaymentRatio")]
        public decimal TotalPurchaseFazaaTopUpMediatorPaymentRatio { get; set; }

        [JsonProperty("totalPurchaseFazaaTopUpExternalTransferRatio")]
        public decimal TotalPurchaseFazaaTopUpExternalTransferRatio { get; set; }
    }

    /// <summary>
    /// بيانات التقرير الشهري
    /// </summary>
    public class MonthlyOperatorCashbackReportData : BaseReportData
    {
        [JsonProperty("results")]
        public List<MonthlyOperatorCashbackReportItem> Results { get; set; } = new();
    }

    /// <summary>
    /// استجابة تقرير الكاش باك الشهري
    /// </summary>
    public class MonthlyOperatorCashbackReportResponse
    {
        [JsonProperty("data")]
        public MonthlyOperatorCashbackReportData Data { get; set; } = new();

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }
}
