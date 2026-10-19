using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Almusher.Models
{
    
        public class AlmusherWalletInfo
        {
            public string WalletId { get; set; } = string.Empty;
            public string WalletIdentifier { get; set; } = string.Empty;
            public int CurrencyId { get; set; }
            public string SubscriptionId { get; set; } = string.Empty;
        }

        // ================================================================
        // ✅ جانب الصرف (Buy / Sell)
        // ================================================================

        public class CurrencyExchangeSide
        {
            public AlmusherWalletInfo CreditorWallet { get; set; } = new();
            public AlmusherWalletInfo DebitorWallet { get; set; } = new();
            public decimal Amount { get; set; }
            public decimal FinalAmount { get; set; }
            public Guid RegionId { get; set; }
            public Guid? HolderId { get; set; }
        }

        // ================================================================
        // ✅ عناصر الربح (Profit Elements)
        // ================================================================

        public class ProfitElementDetails
        {
            public string Description { get; set; } = string.Empty;
            public decimal Ratio { get; set; }
            public decimal Amount { get; set; }
        }

        // ================================================================
        // ✅ معلومات الربح (Profit)
        // ================================================================

        public class ProfitDetails
        {
            public AlmusherWalletInfo Wallet { get; set; } = new();
            public decimal TotalRatio { get; set; }
            public decimal TotalAmount { get; set; }
            public string DetailedStatement { get; set; } = string.Empty;
            public List<ProfitElementDetails> ProfitElements { get; set; } = new();
        }

        // ================================================================
        // ✅ عناصر العمولة (Commission Elements)
        // ================================================================

        public class CommissionElementDetails
        {
            public string Description { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public bool IsIncluded { get; set; }
        }

        // ================================================================
        // ✅ معلومات العمولة (Commission)
        // ================================================================

        public class CommissionDetails
        {
            public AlmusherWalletInfo Wallet { get; set; } = new();
        public decimal TotalIncludedAmount { get; set; }
        public decimal TotalExcludedAmount { get; set; }
        public List<CommissionElementDetails> CommissionElements { get; set; } = new();
        }

        // ================================================================
        // ✅ عناصر الإرجاع (Return Elements)
        // ================================================================

        public class ReturnElementDetails
        {
            public string Description { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }

        // ================================================================
        // ✅ معلومات الإرجاع (Return)
        // ================================================================

        public class ReturnDetails
        {
            public AlmusherWalletInfo Wallet { get; set; } = new();
            public decimal TotalAmount { get; set; }
            public List<ReturnElementDetails> ReturnElements { get; set; } = new();
        }

        // ================================================================
        // ✅ تفاصيل الصرف (Exchange Details Response)
        // ================================================================

        public class ExchangeDetailsResponse
        {
            public string Id { get; set; } = string.Empty;
            public CurrencyExchangeSide CurrencyExchangeBuy { get; set; } = new();
            public CurrencyExchangeSide CurrencyExchangeSell { get; set; } = new();
            public decimal ConversionRate { get; set; }
            public CommissionDetails? Commission { get; set; }
            public string DetailedStatement { get; set; } = string.Empty;
            public string? ReferenceId { get; set; }
            public string? Reason { get; set; }
            public int ExchangeType { get; set; }
            public int Kind { get; set; }
            public ProfitDetails? Profit { get; set; }
            public ReturnDetails? Return { get; set; }
            public decimal LydRate { get; set; }
            public bool UsesSellCurrencyAsBase { get; set; }
            public object? PartialRefund { get; set; }
        }
    }

