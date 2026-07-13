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
                Role = user.Role
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
                var (walletId, subscriptionId, regionId) = await _walletFlow.GetAllIdsAsync(
                    session.UserKey, options.CurrencyType, options.RegionName, options.HolderName,
                    options.SubscriptionType, options.SubscriptionName);

                session.WalletId = walletId.ToString();
                session.SubscriptionId = subscriptionId.ToString();
                session.RegionId = regionId.ToString();
            }

            return session;
        }
    }
}
