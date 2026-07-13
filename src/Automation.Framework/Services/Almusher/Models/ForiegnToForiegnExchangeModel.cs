using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Almusher.Models
{
    public class ForiegnToForiegnEXchangeModel
    {
 
            public string OperationId { get; set; } = string.Empty;
            public string BuyCreditorWalletId { get; set; } = string.Empty;
            public string BuyDebitorWalletId { get; set; } = string.Empty;
            public string SellCreditorWalletId { get; set; } = string.Empty;
            public string SellDebitorWalletId { get; set; } = string.Empty;
            public decimal BuyAmount { get; set; }
            public decimal SellAmount { get; set; }
            public string DetailedStatement { get; set; } = "string";
            public ForiegnToForiegnReturnConfig? Return { get; set; }
            public ForiegnToForiegnCommissionConfig? Commission { get; set; }
            public decimal LydRate { get; set; }
            public bool UsesSellCurrencyAsBase { get; set; }
    }

        public class ForiegnToForiegnReturnConfig
        {
            public string CreditorReturnWalletId { get; set; } = string.Empty;
            public string DebitorReturnWalletId { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public List<ForiegnToForiegnReturnElement> ReturnElements { get; set; } = new();
        }

        public class ForiegnToForiegnReturnElement
        {
            public string Description { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }

        public class ForiegnToForiegnCommissionConfig
        {
            public string WalletId { get; set; } = string.Empty;
            public List<ForiegnToForiegnCommissionElement> CommissionElements { get; set; } = new();
        }

        public class ForiegnToForiegnCommissionElement
        {
            public string Description { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public bool IsIncluded { get; set; }
        }
 }

