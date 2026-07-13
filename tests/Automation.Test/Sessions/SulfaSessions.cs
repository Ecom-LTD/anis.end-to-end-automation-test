using Automation.Framework.Builders;
using Automation.Framework.Configuration;
using Automation.Framework.Constants;
using Automation.Framework.Core.Enums;

namespace Automation.Test.Sessions
{
    public static class SulfaSessions
    {
        public static SessionOptions Dashboard => Build(TestUsers.Dashboard, SubscriptionType.Operator, false , false);
        public static SessionOptions Business => Build(TestUsers.SulfaBusiness, SubscriptionType.Business,true,true);
        public static SessionOptions Operator => Build(TestUsers.SulfaOperator, SubscriptionType.Operator, true,true);


        public static IEnumerable<SessionOptions> NonDashboard => new[] { Business, Operator };

        public static SessionOptions OperatorFor(string userKey) => Build(userKey, SubscriptionType.Operator);
        public static SessionOptions BusinessFor(string userKey) => Build(userKey, SubscriptionType.Business);

        private static SessionOptions Build(string userKey, SubscriptionType type, bool loadAccount = true, bool loadWallet = true) => new()
        {
            UserKey = userKey,
            LoadAccount = loadAccount,
            LoadWallet = loadWallet,
            CurrencyType = CurrencyType.LYD,
            RegionName = "Tripoli",
            HolderName = "Cash",
            SubscriptionType = type,
            SubscriptionName = UserHelper.GetUser(userKey).SubscriptionName
        };
    }
}
