using Automation.Framework.Builders;
using Automation.Framework.Configuration;
using Automation.Framework.Constants;
using Automation.Framework.Core.Enums;

namespace Automation.Test.Sessions
{
    /// <summary>
    /// تعريف جلسات اختبار Almusher
    /// ⚠️ ملاحظة: كل مستخدم له جلسة واحدة فقط مع عملة محددة
    /// </summary>
    public class AlmusherSession
    {
        // ================================================================
        // ✅ تعريف الجلسات - كل جلسة محددة بمستخدم + عملة
        // ================================================================

        /// <summary>
        /// Anis Pay - محفظة LYD
        /// </summary>
        public static SessionOptions AnisPayLyd => Build(
            TestUsers.anispay,
            SubscriptionType.Operator,
            true,
            true,
            CurrencyType.LYD,
            true);

        /// <summary>
        /// Anis Card - محفظة LYD
        /// </summary>
        public static SessionOptions AnisCardLyd => Build(
            TestUsers.aniscard,
            SubscriptionType.Business,
            true,
            true,
            CurrencyType.LYD);

        /// <summary>
        /// Anis Card - محفظة USD
        /// </summary>
        public static SessionOptions AnisCardUsd => Build(
            TestUsers.aniscard,
            SubscriptionType.Business,
            true,
            true,
            CurrencyType.USD,
            true);

        /// <summary>
        /// Hreysh - محفظة USD
        /// </summary>
        public static SessionOptions HreyshUsd => Build(
            TestUsers.hreysh,
            SubscriptionType.Operator,
            true,
            true,
            CurrencyType.USD,
            true);

        /// <summary>
        /// Profit - محفظة LYD
        /// </summary>
        public static SessionOptions ProfitLyd => Build(
     TestUsers.profit,
     SubscriptionType.Business,   // ✅ Business
     true,
     true,
     CurrencyType.LYD,
     autoCreateWallet: true);

        /// <summary>
        /// Commission - محفظة USD
        /// </summary>
        public static SessionOptions CommissionUsd => Build(
            TestUsers.commission,
            SubscriptionType.Operator,
            true,
            true,
            CurrencyType.USD,
        true);

        /// <summary>
        /// Dashboard - بدون محفظة (مشترك بين جميع المشاريع)
        /// ⚠️ LoadWallet = false
        /// </summary>
        public static SessionOptions Dashboard => Build(
            TestUsers.Dashboard,
            SubscriptionType.Operator,
            true,        // LoadAccount = true (مطلوب لجلب AccountId)
            false,       // LoadWallet = false (لا يحتوي على محفظة)
            CurrencyType.LYD);

        // ================================================================
        // ✅ مجموعات الجلسات
        // ================================================================

        /// <summary>
        /// جميع الجلسات غير Dashboard
        /// </summary>
        public static IEnumerable<SessionOptions> NonDashboard => new[]
        {
            AnisPayLyd,
            AnisCardLyd,
            AnisCardUsd,
            HreyshUsd,
            ProfitLyd,
            CommissionUsd
        };

        /// <summary>
        /// جلسات Anis Card (LYD و USD)
        /// </summary>
        public static IEnumerable<SessionOptions> AnisCardSessions => new[]
        {
            AnisCardLyd,
            AnisCardUsd
        };

        /// <summary>
        /// جلسات LYD
        /// </summary>
        public static IEnumerable<SessionOptions> LydSessions => new[]
        {
            AnisPayLyd,
            AnisCardLyd,
            ProfitLyd
        };

        /// <summary>
        /// جلسات USD
        /// </summary>
        public static IEnumerable<SessionOptions> UsdSessions => new[]
        {
            AnisCardUsd,
            HreyshUsd,
            CommissionUsd
        };

        // ================================================================
        // ✅ دالة البناء
        // ================================================================

        private static SessionOptions Build(
            string userKey,
            SubscriptionType type,
            bool loadAccount = true,
            bool loadWallet = true,
            CurrencyType currency = CurrencyType.LYD,
            bool autoCreateWallet = false)
        {
            return new SessionOptions
            {
                UserKey = userKey,
                LoadAccount = loadAccount,
                LoadWallet = loadWallet,
                CurrencyType = currency,
                RegionName = "Tripoli",
                HolderName = "Cash",
                SubscriptionType = type,
                SubscriptionName = UserHelper.GetUser(userKey).SubscriptionName,
                TokenLifetimeMinutes = 50,
                AutoCreateWalletIfNotFound = autoCreateWallet
            };
        }
    }
}