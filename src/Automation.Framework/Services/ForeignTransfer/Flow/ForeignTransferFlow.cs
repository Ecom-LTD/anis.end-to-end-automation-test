using Automation.Framework.Context;
using Automation.Framework.Services.ForeignTransfer.Client;
using Automation.Framework.Services.ForeignTransfer.Models;

namespace Automation.Framework.Services.ForeignTransfer.Flow
{
    public class ForeignTransferFlow
    {
        private readonly ForeignTransferClient _client;
        private readonly StateManager _state;

        public ForeignTransferFlow(ForeignTransferClient client, StateManager state)
        {
            _client = client;
            _state = state;
        }

        /// <summary>
        /// جلب قائمة التحويلات الأجنبية باستخدام جلسة مستخدم معين (غالباً Business User).
        /// </summary>
        public async Task<ForeignTransferResponse> GetForeignTransfersAsync(string userKey)
        {
            var token = _state.GetToken(userKey);
            return await _client.GetForeignTransfersAsync(token);
        }
    }
}