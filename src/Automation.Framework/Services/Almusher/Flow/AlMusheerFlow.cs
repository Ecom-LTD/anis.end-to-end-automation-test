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
        // ✅ 8. بناء طلب صرف أجنبي إلى أجنبي
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
                // ✅ لا نرسل Return و Commission إذا لم تكن مطلوبة
                Return = null,
                Commission = null
            };
        }
     

        // ================================================================
        // ✅ 9. إنشاء Builder لطلب صرف العملات (جديد)
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


        // ================================================================
        // ✅ 10. إنشاء Builder لطلب صرف أجنبي إلى أجنبي (Foreign to Foreign)
        // ================================================================

        public ForeignExchangeRequestBuilder CreateForeignExchangeRequest(
            string operationId,
            string buyCreditorWalletId,
            string buyDebitorWalletId,
            string sellCreditorWalletId,
            string sellDebitorWalletId,
            decimal buyAmount,
            decimal sellAmount,
            decimal lydRate,
            string detailedStatement = "Foreign to foreign exchange"
            )
        {
            return new ForeignExchangeRequestBuilder(
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