using Automation.Framework.Context;
using Automation.Framework.Services.Almusher.Client;
using Automation.Framework.Services.Almusher.Models;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Almusher.Flow
{
    public class AlMusheerFlow
    {
        private readonly AlMusheerClient _alMusheerClient;
        private readonly StateManager _state;

        public AlMusheerFlow(AlMusheerClient alMusheerClient, StateManager state)
        {
            _alMusheerClient = alMusheerClient;
            _state = state;
        }


        public async Task<Guid> CreatePaymentChainAsync(string userKey)
        {
            var token = _state.GetToken(userKey);
            var id = Guid.NewGuid();
            var reference = $"AutoTest-{DateTime.Now:yyyyMMdd-HHmmss}";

            var response = await _alMusheerClient.CreatePaymentChainAsync(token, id, reference);
            var chainId = Guid.Parse(response.Id);

            _state.SetValue($"chain_{userKey}", chainId);
            return chainId;
        }

        /// <summary>
        /// تنفيذ صرف العملات العادي في سلسلة الدفع
        /// </summary>
        public async Task<RegularCurrencyExchangeResponse> RegularCurrencyExchangeAsync(
            string userKey,
            string chainId,
            RegularCurrencyExchangeRequest request)
        {
            var token = _state.GetToken(userKey);
            return await _alMusheerClient.RegularCurrencyExchangeAsync(token, chainId, request);
        }

        /// <summary>
        /// بناء طلب صرف عملات بمعلومات أساسية
        /// </summary>
        public RegularCurrencyExchangeRequest BuildExchangeRequest(
            string operationId,
            string buyCreditorWalletId,
            string buyDebitorWalletId,
            string sellCreditorWalletId,
            string sellDebitorWalletId,
            decimal buyAmount,
            decimal sellAmount,
            string detailedStatement = "Currency exchange operation")
        {
            return new RegularCurrencyExchangeRequest
            {
                OperationId = operationId,
                BuyCreditorWalletId = buyCreditorWalletId,
                BuyDebitorWalletId = buyDebitorWalletId,
                SellCreditorWalletId = sellCreditorWalletId,
                SellDebitorWalletId = sellDebitorWalletId,
                BuyAmount = buyAmount,
                SellAmount = sellAmount,
                DetailedStatement = detailedStatement
            };
        }

        /// <summary>
        /// إضافة ربح إلى الطلب
        /// </summary>
        public RegularCurrencyExchangeRequest AddProfit(
            RegularCurrencyExchangeRequest request,
            string profitWalletId,
            decimal totalRatio,
            string profitDetailedStatement = "Profit from exchange",
            params (string description, decimal ratio)[] profitElements)
        {
            request.Profit = new ProfitInfo
            {
                ProfitWalletId = profitWalletId,
                ProfitDetailedStatement = profitDetailedStatement,
                TotalRatio = totalRatio,
                ProfitElements = profitElements.Select(p => new ProfitElement
                {
                    Description = p.description,
                    Ratio = p.ratio
                }).ToList()
            };

            return request;
        }

        /// <summary>
        /// إضافة إرجاع إلى الطلب
        /// </summary>
        public RegularCurrencyExchangeRequest AddReturn(
            RegularCurrencyExchangeRequest request,
            string returnWalletId,
            decimal totalAmount,
            params (string description, decimal amount)[] returnElements)
        {
            request.Return = new ReturnInfo
            {
                ReturnWalletId = returnWalletId,
                TotalAmount = totalAmount,
                ReturnElements = returnElements.Select(r => new ReturnElement
                {
                    Description = r.description,
                    Amount = r.amount
                }).ToList()
            };

            return request;
        }

        /// <summary>
        /// إضافة عمولة إلى الطلب
        /// </summary>
        public RegularCurrencyExchangeRequest AddCommission(
            RegularCurrencyExchangeRequest request,
            string walletId,
            params (string description, decimal amount, bool isIncluded)[] commissionElements)
        {
            request.Commission = new CommissionInfo
            {
                WalletId = walletId,
                CommissionElements = commissionElements.Select(c => new CommissionElement
                {
                    Description = c.description,
                    Amount = c.amount,
                    IsIncluded = c.isIncluded
                }).ToList()
            };

            return request;
        }
    }
}
