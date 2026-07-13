using Automation.Framework.Context;
using Automation.Framework.Services.Transfer.Client;
using Automation.Framework.Services.Transfer.Models;

namespace Automation.Framework.Services.Transfer.Flow
{
    public class TransferFlow
    {
        private readonly TransferClient _transferClient;
        private readonly StateManager _state;

        public TransferFlow(TransferClient transferClient, StateManager state)
        {
            _transferClient = transferClient;
            _state = state;
        }

        public Task<TransferResponse> TransferAsync(
            string userKey, string fromWalletId, string toSubscriptionId,
            decimal amount, string destinationRegionId)
        {
            var token = _state.GetToken(userKey);
            var request = new TransferRequest
            {
                WalletId = fromWalletId,
                SubscriptionId = toSubscriptionId,
                Value = amount,
                DestinationRegionId = destinationRegionId,
                PinNumber = "001100",
                Note = "string",
                DetailedStatement = "string",
                AnisCardOperationType = 0
            };
            return _transferClient.TransferAsync(token, request);
        }
    }
}
