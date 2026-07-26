using Automation.Framework.Services.Almusher.Models;
using System;
using System.Collections.Generic;

namespace Automation.Framework.Services.Almusher.Flow
{
    /// <summary>
    /// ✅ Builder لإنشاء طلب صرف العملات (Regular Exchange)
    /// يدعم: أرباح، عمولات، إرجاع، أو بدون أي منها
    /// </summary>
    public class ExchangeRequestBuilder
    {
        // ════════════════════════════════════════════════════════════
        // 1. المتغيرات الداخلية (الخاصة)
        // ════════════════════════════════════════════════════════════

        private readonly RegularCurrencyExchangeRequest _request;
        private bool _hasProfit;
        private bool _hasCommission;
        private bool _hasReturn;

        // ════════════════════════════════════════════════════════════
        // 2. المُنشئ (Constructor) - القيم الأساسية المطلوبة
        // ════════════════════════════════════════════════════════════

        public ExchangeRequestBuilder(
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
            // ✅ إنشاء الطلب الأساسي (بدون أي إضافات)
            _request = new RegularCurrencyExchangeRequest
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
                UsesSellCurrencyAsBase = false,
                Profit = null,
                Commission = null,
                Return = null
            };
        }

        // ════════════════════════════════════════════════════════════
        // 3. دوال إضافة الخيارات (كل دالة تعيد الـ Builder نفسه)
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// ✅ إضافة ربح (Profit) إلى الطلب - نسخة مبسطة
        /// </summary>
        public ExchangeRequestBuilder WithProfit(
            string profitWalletId,
            decimal totalRatio,
            string profitDetailedStatement = "Profit from exchange",
            string description = "Profit")
        {
            _request.Profit = new ProfitInfo
            {
                ProfitWalletId = profitWalletId,
                ProfitDetailedStatement = profitDetailedStatement,
                TotalRatio = totalRatio,
                ProfitElement = new List<ProfitElement>
              {
                    new ProfitElement
                    {
                        Description = description,
                        Ratio = totalRatio
                    }
                }
            };
            _hasProfit = true;
            return this;
        }

        /// <summary>
        /// ✅ إضافة عمولة (Commission) إلى الطلب - نسخة مبسطة
        /// </summary>
        public ExchangeRequestBuilder WithCommission(
            string walletId,
            decimal amount,
            string description = "Commission",
            bool isIncluded = true)
        {
            _request.Commission = new CommissionInfo
            {
                WalletId = walletId,
                CommissionElements = new List<CommissionElement>
                {
                    new CommissionElement
                    {
                        Description = description,
                        Amount = amount,
                        IsIncluded = isIncluded
                    }
                }
            };
            _hasCommission = true;
            return this;
        }

        /// <summary>
        /// ✅ إضافة إرجاع (Return) إلى الطلب - نسخة مبسطة
        /// </summary>
        public ExchangeRequestBuilder WithReturn(
            string returnWalletId,
            decimal totalAmount,
            string description = "Return",
            decimal? amount = null)
        {
            _request.Return = new ReturnInfo
            {
                ReturnWalletId = returnWalletId,
                TotalAmount = totalAmount,
                ReturnElements = new List<ReturnElement>
                {
                    new ReturnElement
                    {
                        Description = description,
                        Amount = amount ?? totalAmount
                    }
                }
            };
            _hasReturn = true;
            return this;
        }

        /// <summary>
        /// ✅ تعيين قيمة UsesSellCurrencyAsBase
        /// </summary>
        public ExchangeRequestBuilder WithSellCurrencyAsBase(bool usesSellCurrencyAsBase = true)
        {
            _request.UsesSellCurrencyAsBase = usesSellCurrencyAsBase;
            return this;
        }

        // ════════════════════════════════════════════════════════════
        // 4. دالة Build - تنهي البناء وتعيد الطلب النهائي
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// ✅ بناء الطلب النهائي وإعادته
        /// </summary>
        public RegularCurrencyExchangeRequest Build()
        {
            // إذا لم يتم إضافة أي خيارات، نترك Profit و Commission و Return كـ null
            // هذا يمثل طلب صرف بسيط بدون أي إضافات
            return _request;
        }

        /// <summary>
        /// ✅ (اختياري) الحصول على معلومات عن الخيارات المضافة
        /// </summary>
        public (bool HasProfit, bool HasCommission, bool HasReturn) GetOptions()
        {
            return (_hasProfit, _hasCommission, _hasReturn);
        }
    }
}