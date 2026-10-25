using Automation.Framework.Configuration;
using Automation.Framework.Constants;
using Automation.Framework.Context;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Framework.Shared;
using Automation.Framework.Services.Account.Flow;
using Automation.Framework.Services.Identity.Flow;

namespace Automation.Framework.Builders
{
    /// <summary>يبني جلسة كاملة (Token + AccountId + Wallet/Subscription/Region) حسب الخيارات.</summary>
    public class SessionBuilder
    {
        private readonly AuthenticationFlow _authFlow;
        private readonly AccountFlow _accountFlow;
        private readonly WalletFlow _walletFlow;
        private readonly StateManager _state;

        public SessionBuilder(AuthenticationFlow authFlow, AccountFlow accountFlow,
                              WalletFlow walletFlow, StateManager state)
        {
            _authFlow = authFlow;
            _accountFlow = accountFlow;
            _walletFlow = walletFlow;
            _state = state;
        }

        public async Task<TestSession> BuildAsync(SessionOptions options)
        {
            var user = UserHelper.GetUser(options.UserKey);
            var session = new TestSession
            {
                UserKey = options.UserKey,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                SubscriptionName = user.SubscriptionName,
                AutoCreateWalletIfNotFound = options.AutoCreateWalletIfNotFound
            };

            // 1) تسجيل الدخول دائمًا
            await _authFlow.LoginAsAsync(session.UserKey);
            session.Token = _state.GetToken(session.UserKey);

            // 2) AccountId (يُجلب عبر توكن Dashboard)
            if (options.LoadAccount)
            {
                var dashboardToken = _state.GetToken(TestUsers.Dashboard);
                var accountId = await _accountFlow.GetAccountIdByPhoneNumberUsingTokenAsync(
                    dashboardToken, session.PhoneNumber);
                session.AccountId = accountId.ToString();
            }

            // 3) Wallet + Subscription + Region
            if (options.LoadWallet)
            {
                // ✅ لا try/catch — نترك الاستثناء يظهر بوضوح
                var (walletId, subscriptionId, regionId) = await _walletFlow.GetAllIdsAsync(
                    session.UserKey,
                    options.CurrencyType,
                    options.RegionName,
                    options.HolderName,
                    options.SubscriptionType,
                    options.SubscriptionName,
                    options.AutoCreateWalletIfNotFound);

                if (walletId == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        $"Wallet '{options.CurrencyType}/{options.RegionName}/{options.HolderName}' " +
                        $"not found for user '{session.UserKey}'. " +
                        $"Subscription: '{options.SubscriptionName}' " +
                        $"(type: {options.SubscriptionType}). " +
                        $"Check these values in appsettings.json vs the user's actual subscriptions.");
                }

                session.WalletId = walletId.ToString();
                session.SubscriptionId = subscriptionId.ToString();
                session.RegionId = regionId.ToString();
            }
            else
            {
                // ✅ حتى إذا كان LoadWallet = false، نحاول جلب SubscriptionId
                await LoadSubscriptionIdOnly(session, options);
            }

            return session;
        }


        /// <summary>
        /// ✅ جلب SubscriptionId فقط (بدون محفظة)
        /// </summary>
        private async Task LoadSubscriptionIdOnly(TestSession session, SessionOptions options)
        {
            try
            {
                var profile = await _walletFlow.GetProfileAsync(session.UserKey);

                var subscription = profile.Data.Subscriptions
                    .FirstOrDefault(s =>
                        s.SubscriptionType == options.SubscriptionType &&
                        s.SubscriptionName == options.SubscriptionName)
                    ?? profile.Data.Subscriptions.FirstOrDefault();

                if (subscription != null)
                {
                    session.SubscriptionId = subscription.SubscriptionId.ToString();
                    session.SubscriptionName = subscription.SubscriptionName;

                    // ✅ تخزين SubscriptionId في State للاستخدام لاحقاً
                    _state.SetValue(StateKeys.SubscriptionId(session.UserKey, options.SubscriptionType), subscription.SubscriptionId);

                    Console.WriteLine($"✅ SubscriptionId loaded for {session.UserKey}: {session.SubscriptionId}");
                }
                else
                {
                    Console.WriteLine($"⚠️ No subscription found for {session.UserKey}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load SubscriptionId for {session.UserKey}: {ex.Message}");
            }
        }
    }
}
    

