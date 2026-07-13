using Automation.Framework.Builders;
using Automation.Framework.Configuration;
using Automation.Framework.Constants;
using Automation.Framework.Core.Enums;

namespace Automation.Test.Sessions
{
    public class AlmusherSession
    {
        // ================================================================
        // ✅ AlMusheer Users
        // ================================================================

        public static SessionOptions anispay => Build(TestUsers.anispay, SubscriptionType.Operator, true, true);
        public static SessionOptions aniscard => Build(TestUsers.aniscard, SubscriptionType.Business, true, true);
        public static SessionOptions hreysh => Build(TestUsers.hreysh, SubscriptionType.Operator, true, true);
        public static SessionOptions profit => Build(TestUsers.profit, SubscriptionType.Business, true, true);
        public static SessionOptions commission => Build(TestUsers.commission, SubscriptionType.Operator, true, true);
        public static SessionOptions dashboard => Build(TestUsers.Dashboard, SubscriptionType.Operator, false, false);

        // ================================================================
        // ✅ Anis Card Sessions (مخصصة لجلب المحافظ بالعملات المختلفة)
        // ================================================================

        /// <summary>
        /// جلسة Anis Card مع محفظة LYD (CurrencyType = 1)
        /// </summary>
        public static SessionOptions AnisCardLyd => Build(
            TestUsers.aniscard,
            SubscriptionType.Business,
            true,
            true,
            CurrencyType.LYD);  // ✅ LYD

        /// <summary>
        /// جلسة Anis Card مع محفظة USD (CurrencyType = 2)
        /// </summary>
        public static SessionOptions AnisCardUsd => Build(
            TestUsers.aniscard,
            SubscriptionType.Business,
            true,
            true,
            CurrencyType.USD);  // ✅ USD

        public static IEnumerable<SessionOptions> NonDashboard => new[]
        {
            anispay,
            aniscard,
            hreysh,
            profit,
            commission
        };

        /// <summary>
        /// جميع جلسات Anis Card (LYD و USD)
        /// </summary>
        public static IEnumerable<SessionOptions> AnisCardSessions => new[]
        {
            AnisCardLyd,
            AnisCardUsd
        };

        // ================================================================
        // ✅ دالة البناء (معدلة لدعم العملة)
        // ================================================================

        private static SessionOptions Build(
            string userKey,
            SubscriptionType type,
            bool loadAccount = true,
            bool loadWallet = true,
            CurrencyType currency = CurrencyType.LYD)  // ✅ معامل العملة
        {
            return new SessionOptions
            {
                UserKey = userKey,
                LoadAccount = loadAccount,
                LoadWallet = loadWallet,
                CurrencyType = currency,  // ✅ استخدام العملة المحددة
                RegionName = "Tripoli",
                HolderName = "Cash",
                SubscriptionType = type,
                SubscriptionName = UserHelper.GetUser(userKey).SubscriptionName
            };
        }
    }
}