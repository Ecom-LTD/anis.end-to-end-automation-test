using Automation.Framework.Core.Enums;

namespace Automation.Framework.Constants
{
    public static class StateKeys
    {
        public static string Token(string userKey) => $"token_{userKey}";
        public static string Wallet(string userKey, CurrencyType c) => $"{userKey}_wallet_{c}";
        public static string RegionId(string userKey, CurrencyType c) => $"{userKey}_region_{c}";
        public static string SubscriptionId(string userKey, SubscriptionType t) => $"{userKey}_subscription_{t}";
        public static string AccountId(string userKey, string phone) => $"{userKey}_{phone}_account_id";
    }
}
