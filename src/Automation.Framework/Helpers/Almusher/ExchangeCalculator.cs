using Automation.Framework.Helpers.Almusher;

namespace Automation.Framework.Helpers.Almusher
{
    public static class ExchangeCalculator
    {
        public static decimal CalcRate(decimal sellAmount, decimal buyAmount)
        {
            return DecimalComparer.Truncate(sellAmount / buyAmount, 10);
        }

        public static SimpleExchangeResult SimpleExchange(decimal buyUsd, decimal sellLyd)
        {
            var rate = CalcRate(sellLyd, buyUsd);
            var lydCost = DecimalComparer.Truncate(rate * buyUsd, 10);

            return new SimpleExchangeResult
            {
                BuyAmount = buyUsd,
                SellAmount = sellLyd,
                Rate = rate,
                LydCost = lydCost
            };
        }

        public static ProfitExchangeResult ProfitExchange(
            decimal buyUsd,
            decimal sellLyd,
            decimal profitRatio)
        {
            var baseRate = CalcRate(sellLyd, buyUsd);
            var profitRaw = (profitRatio + baseRate) * buyUsd - sellLyd;
            var profitValue = Math.Floor(profitRaw * 1000m) / 1000m;
            var finalSell = sellLyd + profitValue;
            var finalRate = DecimalComparer.Truncate(finalSell / buyUsd, 10);

            return new ProfitExchangeResult
            {
                BuyAmount = buyUsd,
                SellAmount = sellLyd,
                BaseRate = baseRate,
                ProfitValue = profitValue,
                FinalRate = finalRate
            };
        }

        public static decimal CalcNewAvgRate(
            decimal oldBalance,
            decimal oldEstLyd,
            decimal finalBuyAmount,
            decimal newLydCost)
        {
            var newBalance = oldBalance + finalBuyAmount;
            var newEstLyd = newLydCost + oldEstLyd;
            return DecimalComparer.Truncate(newEstLyd / newBalance, 10);
        }
    }

    public class SimpleExchangeResult
    {
        public decimal BuyAmount { get; set; }
        public decimal SellAmount { get; set; }
        public decimal Rate { get; set; }
        public decimal LydCost { get; set; }
    }

    public class ProfitExchangeResult
    {
        public decimal BuyAmount { get; set; }
        public decimal SellAmount { get; set; }
        public decimal BaseRate { get; set; }
        public decimal ProfitValue { get; set; }
        public decimal FinalRate { get; set; }
    }
}