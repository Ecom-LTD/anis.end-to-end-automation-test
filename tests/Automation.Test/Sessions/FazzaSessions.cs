using Automation.Framework.Constants;
using Automation.Framework.Core.Enums;
using System;
using Automation.Framework.Builders;
using Automation.Framework.Configuration;

namespace Automation.Test.Sessions
{
    public class FazzaSessions
    {

        /// <summary>
        /// Dashboard - مشرف النظام (يبقى ثابتاً لأنه يحتاج صلاحيات الإدارة).
        /// </summary>
        public static SessionOptions Dashboard => Build(
            userKey: TestUsers.Dashboard,
            type: SubscriptionType.Operator,
            loadAccount: false,   // Dashboard لا يحتاج AccountId
            loadWallet: false);

        /// <summary>
        /// مستخدم الفزعة الأول (Fazza1) - بصفته Operator.
        /// </summary>
        public static SessionOptions FazzaUser1 => Build(
            userKey: TestUsers.FazzaUser1,
            type: SubscriptionType.Operator,
            loadAccount: true,
            loadWallet: true);

        /// <summary>
        /// مستخدم الفزعة الثاني (Fazza2) - بصفته Operator (للاستخدام المتوازي أو السيناريوهات المختلفة).
        /// </summary>
        public static SessionOptions FazzaUser2 => Build(
            userKey: TestUsers.FazzaUser2,
            type: SubscriptionType.Business,
            loadAccount: true,
            loadWallet: true);
        // ================================================================
        // مجموعات الجلسات (للبناء بالتوازي)
        // ================================================================

        /// <summary>
        /// كل الجلسات ما عدا Dashboard.
        /// </summary>
        public static IEnumerable<SessionOptions> NonDashboard => new[]
        {
        FazzaUser1,
        FazzaUser2
    };
        // ================================================================
        // دالة البناء الداخلية
        // ================================================================

        private static SessionOptions Build(
            string userKey,
            SubscriptionType type,
            bool loadAccount,
            bool loadWallet)
        {
            var user = UserHelper.GetUser(userKey);

            return new SessionOptions
            {
                UserKey = userKey,
                LoadAccount = loadAccount,
                LoadWallet = loadWallet,
                CurrencyType = CurrencyType.LYD,
                RegionName = "Tripoli",
                HolderName = "Cash",
                SubscriptionType = type,
                SubscriptionName = user.SubscriptionName,
                TokenLifetimeMinutes = 50,
                AutoCreateWalletIfNotFound = true // مهم جداً لتجنب فشل الجلسة إذا لم تكن المحفظة موجودة
            };

        }
    }
}
