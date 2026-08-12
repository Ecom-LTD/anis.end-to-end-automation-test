namespace Automation.Framework.Shared
{
    public class TestSession
    {
        public string UserKey { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string WalletId { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public string RegionId { get; set; } = string.Empty;
        // ✅ إضافة الخاصية المفقودة
        public string SubscriptionName { get; set; } = string.Empty;

        // ✅ إضافة الخاصية المفقودة (للتحكم في الإنشاء التلقائي)
        public bool AutoCreateWalletIfNotFound { get; set; } = false;
        public string Role { get; set; } = string.Empty;

        public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
        public bool HasWallet => !string.IsNullOrEmpty(WalletId);

        public Guid AccountIdGuid => Guid.TryParse(AccountId, out var id) ? id : Guid.Empty;
        public Guid WalletIdGuid => Guid.TryParse(WalletId, out var id) ? id : Guid.Empty;
        public Guid SubscriptionIdGuid => Guid.TryParse(SubscriptionId, out var id) ? id : Guid.Empty;
        public Guid RegionIdGuid => Guid.TryParse(RegionId, out var id) ? id : Guid.Empty;

        public void CopyFrom(TestSession other)
        {
            Token = other.Token;
            AccountId = other.AccountId;
            WalletId = other.WalletId;
            SubscriptionId = other.SubscriptionId;
            RegionId = other.RegionId;
            PhoneNumber = other.PhoneNumber;
            SubscriptionName = other.SubscriptionName;
            AutoCreateWalletIfNotFound = other.AutoCreateWalletIfNotFound;
        }

        public override string ToString() => $"[{UserKey}] {PhoneNumber}";
    }
}
