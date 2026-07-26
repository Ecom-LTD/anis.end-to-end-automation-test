using Automation.Framework.Context;
using Automation.Framework.Core.Http;
using Automation.Framework.Services.Almusher.Client;
using Automation.Framework.Services.Almusher.Models;
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

        // ================================================================
        // ✅ 1. إنشاء سلسلة دفع
        // ================================================================

        public async Task<ApiResponse<CreatePaymentChainResponse>> CreatePaymentChainAsync(string userKey)
        {
            var token = _state.GetToken(userKey);

            var request = new CreatePaymentChainRequest
            {
                Id = Guid.NewGuid(),
                Reference = $"AutoTest-{DateTime.Now:yyyyMMdd-HHmmss}",
                Statement = "Automated E2E Test"
            };

            return await _alMusheerClient.CreatePaymentChainAsync(token, request);
        }

        // ================================================================
        // ✅ 2. تنفيذ صرف العملات العادي
        // ================================================================

        public async Task<ApiResponse<RegularCurrencyExchangeResponse>> RegularCurrencyExchangeAsync(
            string userKey,
            string chainId,
            RegularCurrencyExchangeRequest request)
        {
            var token = _state.GetToken(userKey);
            return await _alMusheerClient.RegularCurrencyExchangeAsync(token, chainId, request);
        }

        // ================================================================
        // ✅ 3. جلب تفاصيل الصرف
        // ================================================================

        public async Task<ApiResponse<ExchangeDetailsResponse>> GetExchangeDetailsAsync(
            string userKey,
            string chainId,
            string operationId)
        {
            var token = _state.GetToken(userKey);
            return await _alMusheerClient.GetExchangeDetailsAsync(token, chainId, operationId);
        }

        // ================================================================
        // ✅ 4. جلب معلومات متوسط السعر
        // ================================================================

        public async Task<ApiResponse<AverageRateInfoResponse>> GetAverageRateInfoAsync(
            string userKey,
            string walletId)
        {
            var token = _state.GetToken(userKey);
            return await _alMusheerClient.GetAverageRateInfoAsync(token, walletId);
        }

        // ================================================================
        // ✅ 5. تأكيد الصرف
        // ================================================================

        public async Task<ApiResponse<ConfirmCurrencyExchangeResponse>> ConfirmExchangeAsync(
            string userKey,
            string chainId,
            string operationId)
        {
            var token = _state.GetToken(userKey);
            return await _alMusheerClient.ConfirmCurrencyExchangeAsync(token, chainId, operationId);
        }

        // ================================================================
        // ✅ 6. صرف أجنبي إلى أجنبي
        // ================================================================

        public async Task<ApiResponse<ForeignToForeignExchangeResponse>> ForeignToForeignExchangeAsync(
            string userKey,
            string chainId,
            ForeignToForeignExchangeRequest request)
        {
            var token = _state.GetToken(userKey);
            return await _alMusheerClient.ForeignToForeignExchangeAsync(token, chainId, request);
        }

        // ================================================================
        // ✅ 7. الحصول على نسبة USD
        // ================================================================

        public async Task<ApiResponse<UsdRatioResponse>> GetUsdRatioAsync(
            string userKey,
            string chainId,
            string operationId)
        {
            var token = _state.GetToken(userKey);
            return await _alMusheerClient.GetUsdRatioAsync(token, chainId, operationId);
        }

        // ================================================================
        // ✅ 8. بناء طلب صرف العملات
        // ================================================================

        public RegularCurrencyExchangeRequest BuildExchangeRequest(
            string operationId,
            string buyCreditorWalletId,
            string buyDebitorWalletId,
            string sellCreditorWalletId,
            string sellDebitorWalletId,
            decimal buyAmount,
            decimal sellAmount,
            decimal lydRate,
            bool usesSellCurrencyAsBase = false,
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
                DetailedStatement = detailedStatement,
                LydRate = lydRate,
                UsesSellCurrencyAsBase = usesSellCurrencyAsBase
            };
        }

        // ================================================================
        // ✅ 9. إضافة ربح
        // ================================================================

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
                ProfitElement = profitElements.Select(p => new ProfitElement
                {
                    Description = p.description,
                    Ratio = p.ratio
                }).ToList()
            };

            return request;
        }

        // ================================================================
        // ✅ 10. إضافة إرجاع
        // ================================================================

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

        // ================================================================
        // ✅ 11. إضافة عمولة
        // ================================================================

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

        // ================================================================
        // ✅ 12. بناء طلب صرف أجنبي إلى أجنبي
        // ================================================================

        public ForeignToForeignExchangeRequest BuildForeignToForeignRequest(
            string operationId,
            string buyCreditorWalletId,
            string buyDebitorWalletId,
            string sellCreditorWalletId,
            string sellDebitorWalletId,
            decimal buyAmount,
            decimal sellAmount,
            decimal lydRate,
            bool usesSellCurrencyAsBase = false,
            string detailedStatement = "Foreign to foreign exchange")
        {
            return new ForeignToForeignExchangeRequest
            {
                OperationId = operationId,
                BuyCreditorWalletId = buyCreditorWalletId,
                BuyDebitorWalletId = buyDebitorWalletId,
                SellCreditorWalletId = sellCreditorWalletId,
                SellDebitorWalletId = sellDebitorWalletId,
                BuyAmount = buyAmount,
                SellAmount = sellAmount,
                LydRate = lydRate,
                UsesSellCurrencyAsBase = usesSellCurrencyAsBase,
                DetailedStatement = detailedStatement,
                Return = new ForeignToForeignReturnConfig(),
                Commission = new ForeignToForeignCommissionConfig()
            };
        }

        // ================================================================
        // ✅ 13. إضافة إرجاع لطلب أجنبي إلى أجنبي
        // ================================================================

        public ForeignToForeignExchangeRequest AddForeignToForeignReturn(
            ForeignToForeignExchangeRequest request,
            string creditorReturnWalletId,
            string debitorReturnWalletId,
            decimal totalAmount,
            params (string description, decimal amount)[] returnElements)
        {
            request.Return = new ForeignToForeignReturnConfig
            {
                CreditorReturnWalletId = creditorReturnWalletId,
                DebitorReturnWalletId = debitorReturnWalletId,
                TotalAmount = totalAmount,
                ReturnElements = returnElements.Select(r => new ForeignToForeignReturnElement
                {
                    Description = r.description,
                    Amount = r.amount
                }).ToList()
            };

            return request;
        }

        // ================================================================
        // ✅ 14. إضافة عمولة لطلب أجنبي إلى أجنبي
        // ================================================================

        public ForeignToForeignExchangeRequest AddForeignToForeignCommission(
            ForeignToForeignExchangeRequest request,
            string walletId,
            params (string description, decimal amount, bool isIncluded)[] commissionElements)
        {
            request.Commission = new ForeignToForeignCommissionConfig
            {
                WalletId = walletId,
                CommissionElements = commissionElements.Select(c => new ForeignToForeignCommissionElement
                {
                    Description = c.description,
                    Amount = c.amount,
                    IsIncluded = c.isIncluded
                }).ToList()
            };

            return request;
        }
        // ================================================================
        // ✅ 15. إنشاء Builder لطلب صرف العملات (جديد)
        // ================================================================

        public ExchangeRequestBuilder CreateExchangeRequest(
            string operationId,
            string buyCreditorWalletId,
            string buyDebitorWalletId,
            string sellCreditorWalletId,
            string sellDebitorWalletId,
            decimal buyAmount,
            decimal sellAmount,
            decimal lydRate,
            string detailedStatement = "Currency exchange operation")
        {
            return new ExchangeRequestBuilder(
                operationId,
                buyCreditorWalletId,
                buyDebitorWalletId,
                sellCreditorWalletId,
                sellDebitorWalletId,
                buyAmount,
                sellAmount,
                lydRate,
                detailedStatement);
        }
    }
}