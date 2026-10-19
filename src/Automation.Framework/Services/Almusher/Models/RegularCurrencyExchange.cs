namespace Automation.Framework.Services.Almusher.Models
{
    /// <summary>
    /// طلب تنفيذ صرف العملات العادي
    /// </summary>
    public class RegularCurrencyExchangeRequest
    {
        public string OperationId { get; set; } = string.Empty;
        public string BuyCreditorWalletId { get; set; } = string.Empty;
        public string BuyDebitorWalletId { get; set; } = string.Empty;
        public string SellCreditorWalletId { get; set; } = string.Empty;
        public string SellDebitorWalletId { get; set; } = string.Empty;
        public decimal BuyAmount { get; set; }
        public decimal SellAmount { get; set; }
        public string DetailedStatement { get; set; } = "string";
        public object? Profit { get; set; } = null;
        public object? Return { get; set; } = null;
        public object? Commission { get; set; } = null;

        // ✅ إضافة الخصائص المفقودة
        public decimal LydRate { get; set; }
        public bool UsesSellCurrencyAsBase { get; set; }
    }

    /// <summary>
    /// معلومات الربح
    /// </summary>
    public class ProfitInfo
    {
        public string ProfitWalletId { get; set; } = string.Empty;
        public string ProfitDetailedStatement { get; set; } = "string";
        public decimal TotalRatio { get; set; }
        public List<ProfitElement> ProfitElement { get; set; } = new();
    }

    /// <summary>
    /// عنصر الربح
    /// </summary>
    public class ProfitElement
    {
        public string Description { get; set; } = string.Empty;
        public decimal Ratio { get; set; }
    }

    /// <summary>
    /// معلومات الإرجاع
    /// </summary>
    public class ReturnInfo
    {
        public string ReturnWalletId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<ReturnElement> ReturnElements { get; set; } = new();
    }

    /// <summary>
    /// عنصر الإرجاع
    /// </summary>
    public class ReturnElement
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// معلومات العمولة
    /// </summary>
    public class CommissionInfo
    {
        public string WalletId { get; set; } = string.Empty;

        public List<CommissionElement> CommissionElements { get; set; } = new();
    }

    /// <summary>
    /// عنصر العمولة
    /// </summary>
    public class CommissionElement
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsIncluded { get; set; }
    }

    // ================================================================
    // ✅ استجابة (Response)
    // ================================================================

    /// <summary>
    /// استجابة تنفيذ صرف العملات
    /// </summary>
    public class RegularCurrencyExchangeResponse
    {
        public string Id { get; set; } = string.Empty;

    }



    public class ConfirmCurrencyExchangeResponse
    {
        public string Id { get; set; } = string.Empty;
    }
}