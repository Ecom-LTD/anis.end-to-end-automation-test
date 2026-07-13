using Automation.Framework.Services.Cart.Flow;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Tests.Sulfa.Base;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Sulfa
{
    public class Category : BaseSulfaTest
    {

        // ========== المتغيرات الثابتة ==========
        private const string SubCategoryName = "Razer Gold Global ";
        private const string CardName = "Razer Gold Global $5 ";

        // ========== المُنشئ ==========
        public Category(ITestOutputHelper output, SulfaFixture fixture) : base(output, fixture) { }


        // ========== الاختبارات ==========

        [Fact]
        public async Task GetRazerGoldSubCategoryId_ShouldSucceed()
        {
            // Arrange
            Output.WriteLine("\n🔍 اختبار: جلب معرف الفئة الفرعية");

            // Act
            var subCategoryId = await Catalog.GetSubCategoryIdByNameAsync(
                SulfaBusiness.UserKey,
                SubCategoryName);

            // Assert
            Output.WriteLine($"📋 SubCategory ID: {subCategoryId}");
            Assert.NotNull(subCategoryId);
            Assert.NotEmpty(subCategoryId);

            // Result
            PrintResult(nameof(GetRazerGoldSubCategoryId_ShouldSucceed), true);
        }

        [Fact]
        public async Task GetRazerGoldCardId_ShouldSucceed()
        {
            // Arrange
            Output.WriteLine("\n🔍 اختبار: جلب معرف البطاقة");

            // Act - جلب معرف الفئة الفرعية أولاً
            var subCategoryId = await Catalog.GetSubCategoryIdByNameAsync(
                SulfaBusiness.UserKey,
                SubCategoryName);

            Assert.NotNull(subCategoryId);
            Output.WriteLine($"📋 SubCategory ID: {subCategoryId}");

            // Act - جلب معرف البطاقة
            var cardId = await Catalog.GetCardIdByNameAsync(
                SulfaBusiness.UserKey,
                subCategoryId,
                CardName);

            // Assert
            Output.WriteLine($"📋 Card ID: {cardId}");
            Assert.NotNull(cardId);
            Assert.NotEmpty(cardId);

            // Result
            PrintResult(nameof(GetRazerGoldCardId_ShouldSucceed), true);
        }

        [Fact]
        public async Task GetCardDetails_ByCardId_ShouldSucceed()
        {
            Output.WriteLine("\n🔍 اختبار: جلب تفاصيل البطاقة باستخدام CardId");

            // 1. أولاً: جلب معرف البطاقة
            var subCategoryId = await Catalog.GetSubCategoryIdByNameAsync(
                SulfaBusiness.UserKey, SubCategoryName);

            Assert.NotNull(subCategoryId);

            var cardId = await Catalog.GetCardIdByNameAsync(
                SulfaBusiness.UserKey, subCategoryId, CardName);

            Assert.NotNull(cardId);
            Output.WriteLine($"📋 Card ID: {cardId}");

            // 2. جلب تفاصيل البطاقة
            var cardDetails = await Catalog.GetCardDetailsAsync(
                SulfaBusiness.UserKey, cardId);

            // 3. التحقق من البيانات
            Output.WriteLine($"📋 Card Name: {cardDetails.Name}");
            Output.WriteLine($"📋 Arabic Name: {cardDetails.ArabicName}");
            Output.WriteLine($"📋 Price: {cardDetails.Price}");
            Output.WriteLine($"📋 Business Price: {cardDetails.BusinessPrice}");
            Output.WriteLine($"📋 Face Value: {cardDetails.FaceValue}");
            Output.WriteLine($"📋 In Stock: {cardDetails.InStock}");
            Output.WriteLine($"📋 Logo: {cardDetails.Logo}");

            Assert.NotNull(cardDetails.Id);
            Assert.Equal(CardName.Trim(), cardDetails.Name.Trim());
            Assert.True(cardDetails.Price > 0);
            Assert.True(cardDetails.InStock);

            PrintResult(nameof(GetCardDetails_ByCardId_ShouldSucceed), true);
        }


        [Fact]
        public async Task GetCardDetails_CheckPrice_BeforePurchase()
        {
            Output.WriteLine("\n💰 اختبار: التحقق من سعر البطاقة قبل الشراء");

            // 1. جلب تفاصيل البطاقة
            var cardDetails = await Catalog.GetCardDetailsByNameAsync(
                SulfaBusiness.UserKey,
                SubCategoryName,
                CardName);

            Output.WriteLine($"📋 سعر البطاقة: {cardDetails.Price}");
            Output.WriteLine($"📋 سعر الأعمال: {cardDetails.BusinessPrice}");
            Output.WriteLine($"📋 متوفرة: {(cardDetails.InStock ? "نعم ✅" : "لا ❌")}");

            // 2. جلب رصيد المحفظة
            var balance = await Wallet.GetBalanceAsync(
                SulfaBusiness.UserKey,
                SulfaBusiness.WalletIdGuid);

            Output.WriteLine($"💰 رصيد المحفظة: {balance}");

            // 3. التحقق من كفاية الرصيد
            if (balance >= cardDetails.Price)
            {
                Output.WriteLine("✅ الرصيد كافٍ للشراء");
            }
            else
            {
                Output.WriteLine($"⚠️ الرصيد غير كافٍ. تحتاج: {cardDetails.Price - balance} إضافية");
            }

            PrintResult(nameof(GetCardDetails_CheckPrice_BeforePurchase), true);
        }

        /// <summary>
        /// //Cart Test
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task DeleteCart_ShouldSucceed()
        {
            // Arrange & Act
            Output.WriteLine("\n🗑️ اختبار: مسح سلة المشتريات");

            var result = await Cart.DeleteAllCartItemsAsync(SulfaOperator.UserKey);

            // Assert
            Output.WriteLine($"📋 نتيجة المسح: {(result ? "نجح ✅" : "فشل ❌")}");
            Assert.True(result, "فشلت عملية مسح السلة");

            // Result
            PrintResult(nameof(DeleteCart_ShouldSucceed), true);
        }

        [Fact]
        public async Task AddRazerGoldToCart_ShouldSucceed()
        {
            Output.WriteLine("\n🛒 اختبار: إضافة بطاقة Razer Gold إلى السلة");

            // Act - تفريغ السلة أولاً
            await Cart.DeleteAllCartItemsAsync(SulfaBusiness.UserKey);
            Output.WriteLine("📋 تم تفريغ السلة");

            // Act - إضافة البطاقة إلى السلة (مع الكمية)
            var (itemId, totalValue) = await Cart.AddCardToCartAsync(
                SulfaBusiness.UserKey,
                SubCategoryName,
                CardName,
                quantity: 1);  // ✅ يمكن تحديد الكمية

            // Assert
            Output.WriteLine($"📋 Item ID: {itemId}");
            Output.WriteLine($"📋 Total Value: {totalValue}");
            Assert.NotNull(itemId);
            Assert.NotEmpty(itemId);
            Assert.True(totalValue > 0);

            PrintResult(nameof(AddRazerGoldToCart_ShouldSucceed), true);
        }
        [Fact]
        public async Task PurchaseRazerGoldCard_ShouldSucceed()
        {
            await Cart.DeleteAllCartItemsAsync(SulfaOperator.UserKey);
            Output.WriteLine("📋 تم تفريغ السلة");

            Output.WriteLine("\n💳 اختبار: شراء بطاقة Razer Gold");

            var result = await Cart.PurchaseCardAsync(
                userKey: SulfaBusiness.UserKey,
                walletId: SulfaBusiness.WalletId,
                subCategoryName: SubCategoryName,
                cardName: CardName,
                quantity: 1);

            // ✅ الآن Success ستحسب من Message
            Output.WriteLine($"📋 Response Message: {result.Message}");
            Output.WriteLine($"📋 Success: {result.Success}");
            Output.WriteLine($"📋 Order ID: {result.Data?.OrderId}");
            Output.WriteLine($"📋 Order Number: {result.Data?.Number}");
            Output.WriteLine($"📋 Cards Count: {result.Data?.Cards.Count}");

            if (result.Data?.Cards != null && result.Data.Cards.Any())
            {
                var firstCard = result.Data.Cards.First();
                Output.WriteLine($"📋 Card Name: {firstCard.Card?.Name}");
                Output.WriteLine($"📋 Secret Number: {firstCard.SecretNumber}");
            }

            // ✅ الآن هذا النجاح سيعمل
            Assert.True(result.Success, $"فشلت عملية الشراء: {result.Message}");
            Assert.NotNull(result.Data);
            Assert.NotNull(result.Data.OrderId);
            Assert.NotEmpty(result.Data.OrderId);

            PrintResult(nameof(PurchaseRazerGoldCard_ShouldSucceed), true);
        }
    }
}