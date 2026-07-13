using Automation.Framework.Core.Enums;
using Newtonsoft.Json;

namespace Automation.Framework.Services.Wallet.Models
{
    public class ProfileResponse { public ProfileData Data { get; set; } = new(); }
    public class ProfileData { public List<Subscription> Subscriptions { get; set; } = new(); }

    public class Subscription
    {
        public Guid SubscriptionId { get; set; }
        public string SubscriptionName { get; set; } = string.Empty;
        public SubscriptionType SubscriptionType { get; set; }
        public List<WalletInfo> Wallets { get; set; } = new();
    }

    public class WalletInfo
    {
        public Guid WalletId { get; set; }
        public Guid RegionId { get; set; }
        public string RegionName { get; set; } = string.Empty;
        public string HolderName { get; set; } = string.Empty;
        public CurrencyType CurrencyType { get; set; }
    }

    public class BalanceResponse { public decimal Data { get; set; } }

    public class CreateWallet
    {
        public Guid SubscriptionId { get; set; }
        public Guid RegionId { get; set; }
        public Guid HolderId { get; set; }
        public CurrencyType CurrencyType { get; set; }

    }

    public class UpdateDefaultWalletRequest
    {
        [JsonProperty("walletId")]
        public string WalletId { get; set; } = string.Empty;
    }
    public class TransferRequest
    {
        public string FromWalletId { get; set; } = string.Empty;
        public string ToSubscriptionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string DestinationRegionId { get; set; } = string.Empty;
        public string PinNumber { get; set; } = "001100";
    }

    public class TransferResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

}
