using Automation.Framework.Core.Enums;

namespace Automation.Framework.Builders
{
    public class SessionOptions
    {
        public string UserKey { get; set; } = string.Empty;
        public bool LoadAccount { get; set; } = true;
        public bool LoadWallet { get; set; } = false;

        public CurrencyType CurrencyType { get; set; } = CurrencyType.LYD;
        public string RegionName { get; set; } = "Tripoli";
        public string HolderName { get; set; } = "Cash";
        public SubscriptionType SubscriptionType { get; set; }
        public string SubscriptionName { get; set; } = string.Empty;

        /// <summary>مدة صلاحية التوكن بالدقائق (للكاش). أقل قليلًا من الـ 60 الفعلية.</summary>
        public int TokenLifetimeMinutes { get; set; } = 50;
    }
}
