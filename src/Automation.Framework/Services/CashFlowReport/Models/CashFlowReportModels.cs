

namespace Automation.Framework.Services.CashFlowReport.Models
{


        /// <summary>
        /// استجابة تقرير التدفق النقدي (Cashflow Report)
        /// </summary>
        public class CashflowReportResponse
        {
            public List<CashflowReportItem> Results { get; set; } = new();
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
            public int Total { get; set; }
            public int LastPage { get; set; }
        }

        /// <summary>
        /// عنصر واحد في تقرير التدفق النقدي
        /// </summary>
        public class CashflowReportItem
        {
            public long Id { get; set; }
            public string RecipientWalletId { get; set; } = string.Empty;
            public string SubscriptionName { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string AccountLocation { get; set; } = string.Empty;
            public string WalletRegion { get; set; } = string.Empty;
            public int Urgent { get; set; }
            public int Delay { get; set; }
            public decimal Balance { get; set; }
            public decimal AverageSales { get; set; }
            public decimal PositiveBalanceRate { get; set; }
            public DateTime ConfirmedAt { get; set; }
            public DateTime? LastDelayRefundDate { get; set; }
            public string OperatorName { get; set; } = string.Empty;
            public decimal TotalSalesLastDay { get; set; }
            public string AccountState { get; set; } = string.Empty;
            public string? RemainingTime { get; set; }
            public string AccountId { get; set; } = string.Empty;
            public string? HolderName { get; set; }
            public string WalletType { get; set; } = string.Empty;
            public decimal TotalBalance { get; set; }
            public decimal? LastDelayRefundAmount { get; set; }
            public decimal CurrentFazaa { get; set; }
            public decimal CurrentSulfa { get; set; }
            public decimal CurrentFazaaDue { get; set; }
            public decimal CurrentFazaaNotDue { get; set; }
            public decimal TotalDue { get; set; }
            public decimal TotalNotDue { get; set; }
            public decimal CurrentDelayedDue { get; set; }
            public bool IsDelayedCovered { get; set; }
            public decimal CurrentSulfaDue { get; set; }
            public bool IsSulfaCovered { get; set; }
            public string AccountLocationCode { get; set; } = string.Empty;
            public string WalletRegionCode { get; set; } = string.Empty;
            public DateTime? ExpiryAt { get; set; }
        }
    }



