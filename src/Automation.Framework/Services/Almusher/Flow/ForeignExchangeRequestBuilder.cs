using Automation.Framework.Services.Almusher.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Automation.Framework.Services.Almusher.Flow
{
    /// <summary>
    /// ✅ Builder لإنشاء طلب صرف أجنبي إلى أجنبي (Foreign to Foreign)
    /// يدعم: عمولات، إرجاع، أو بدون أي منها
    /// </summary>
    public class ForeignExchangeRequestBuilder
    {
        // ════════════════════════════════════════════════════════════
        // 1. المتغيرات الداخلية (الخاصة)
        // ════════════════════════════════════════════════════════════

        private readonly ForeignToForeignExchangeRequest _request;
        private bool _hasCommission;
        private bool _hasReturn;

        // ════════════════════════════════════════════════════════════
        // 2. المُنشئ (Constructor) - القيم الأساسية المطلوبة
        // ════════════════════════════════════════════════════════════

        public ForeignExchangeRequestBuilder(
            string operationId,
            string buyCreditorWalletId,
            string buyDebitorWalletId,
            string sellCreditorWalletId,
            string sellDebitorWalletId,
            decimal buyAmount,
            decimal sellAmount,
            decimal lydRate,
            string detailedStatement = "Foreign to foreign exchange")
        {
            // ✅ إنشاء الطلب الأساسي (بدون أي إضافات)
            _request = new ForeignToForeignExchangeRequest
            {
                OperationId = operationId,
                BuyCreditorWalletId = buyCreditorWalletId,
                BuyDebitorWalletId = buyDebitorWalletId,
                SellCreditorWalletId = sellCreditorWalletId,
                SellDebitorWalletId = sellDebitorWalletId,
                BuyAmount = buyAmount,
                SellAmount = sellAmount,
                LydRate = lydRate,
                UsesSellCurrencyAsBase = false,
                DetailedStatement = detailedStatement,
                Return = null,
                Commission = null
            };
        }

        // ════════════════════════════════════════════════════════════
        // 3. دوال إضافة الخيارات (كل دالة تعيد الـ Builder نفسه)
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// ✅ إضافة عمولة (Commission) إلى الطلب
        /// </summary>
        public ForeignExchangeRequestBuilder WithCommission(
            string walletId,
            params (string description, decimal amount, bool isIncluded)[] commissionElements)
        {
            _request.Commission = new ForeignToForeignCommissionConfig
            {
                WalletId = walletId,
                CommissionElements = commissionElements.Select(c => new ForeignToForeignCommissionElement
                {
                    Description = c.description,
                    Amount = c.amount,
                    IsIncluded = c.isIncluded
                }).ToList()
            };
            _hasCommission = true;
            return this;
        }

        /// <summary>
        /// ✅ إضافة إرجاع (Return) إلى الطلب
        /// </summary>
        public ForeignExchangeRequestBuilder WithReturn(
            string creditorReturnWalletId,
            string debitorReturnWalletId,
            decimal totalAmount,
            params (string description, decimal amount)[] returnElements)
        {
            _request.Return = new ForeignToForeignReturnConfig
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
            _hasReturn = true;
            return this;
        }

        /// <summary>
        /// ✅ تعيين قيمة UsesSellCurrencyAsBase
        /// </summary>
        public ForeignExchangeRequestBuilder WithSellCurrencyAsBase(bool usesSellCurrencyAsBase = true)
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
        public ForeignToForeignExchangeRequest Build()
        {
            return _request;
        }

        /// <summary>
        /// ✅ (اختياري) الحصول على معلومات عن الخيارات المضافة
        /// </summary>
        public (bool HasCommission, bool HasReturn) GetOptions()
        {
            return (_hasCommission, _hasReturn);
        }
    }
}