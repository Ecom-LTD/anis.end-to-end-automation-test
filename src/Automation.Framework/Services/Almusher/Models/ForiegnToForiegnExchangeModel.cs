using System;
using System.Collections.Generic;

namespace Automation.Framework.Services.Almusher.Models
{
    // ================================================================
    // ✅ طلب صرف أجنبي إلى أجنبي (مطابق للـ JSON)
    // ================================================================

    public class ForeignToForeignExchangeRequest
    {
        public string OperationId { get; set; } = string.Empty;
        public string BuyCreditorWalletId { get; set; } = string.Empty;
        public string BuyDebitorWalletId { get; set; } = string.Empty;
        public string SellCreditorWalletId { get; set; } = string.Empty;
        public string SellDebitorWalletId { get; set; } = string.Empty;
        public decimal BuyAmount { get; set; }
        public decimal SellAmount { get; set; }
        public string DetailedStatement { get; set; } = "string";

        // ✅ Return (مطابق للـ JSON)
        public ForeignToForeignReturnConfig Return { get; set; } = new();

        // ✅ Commission (مطابق للـ JSON)
        public ForeignToForeignCommissionConfig Commission { get; set; } = new();

        public decimal LydRate { get; set; }
        public bool UsesSellCurrencyAsBase { get; set; }
    }

    // ================================================================
    // ✅ الإرجاع (Return) - مطابق للـ JSON
    // ================================================================

    public class ForeignToForeignReturnConfig
    {
        public string CreditorReturnWalletId { get; set; } = string.Empty;
        public string DebitorReturnWalletId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<ForeignToForeignReturnElement> ReturnElements { get; set; } = new();
    }

    public class ForeignToForeignReturnElement
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    // ================================================================
    // ✅ العمولة (Commission) - مطابق للـ JSON
    // ================================================================

    public class ForeignToForeignCommissionConfig
    {
        public string WalletId { get; set; } = string.Empty;
        public List<ForeignToForeignCommissionElement> CommissionElements { get; set; } = new();
    }

    public class ForeignToForeignCommissionElement
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsIncluded { get; set; }
    }

    // ================================================================
    // ✅ استجابة صرف أجنبي إلى أجنبي
    // ================================================================

    public class ForeignToForeignExchangeResponse
    {
        public ForeignToForeignExchangeData Data { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    public class ForeignToForeignExchangeData
    {
        public string OperationId { get; set; } = string.Empty;
        public string ChainId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal BuyAmount { get; set; }
        public decimal SellAmount { get; set; }
        public decimal LydRate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}