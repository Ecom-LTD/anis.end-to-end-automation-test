using Automation.Framework.Context;
using Automation.Framework.Services.Account.Client;

namespace Automation.Framework.Services.Account.Flow
{
    public class AccountFlow
    {
        private readonly AccountClient _accountClient;
        private readonly StateManager _state;

        public AccountFlow(AccountClient accountClient, StateManager state)
        {
            _accountClient = accountClient;
            _state = state;
        }

        public Task<Guid> GetAccountIdByPhoneNumberAsync(string userKey, string phoneNumber)
            => _accountClient.GetAccountByPhoneNumberAsync(_state.GetToken(userKey), phoneNumber);

        public Task<Guid> GetAccountIdByPhoneNumberUsingTokenAsync(string token, string phoneNumber)
            => _accountClient.GetAccountByPhoneNumberAsync(token, phoneNumber);
    }
}
