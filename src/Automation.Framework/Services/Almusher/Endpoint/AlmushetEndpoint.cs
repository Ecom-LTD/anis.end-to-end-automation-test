

namespace Automation.Framework.Services.Almusher.Endpoint
{
    public class AlmusherEndpoint
    {

        //Management Endpoints
        public const string InitiatePaymentChain = "/api/v1/al-musheer-simulation/initiate-payment-chain"; public static string GetUsdRatio(System.Guid chainid, System.Guid operationid) => $"/api/v1/al-musheer-simulation/payment-chain/{chainid}/currency-exchange/{operationid}";


        // ========== Payment Chain ==========

        public static string RegularCurrencyExchange(Guid chainId) =>
            $"/api/v1/al-musheer-simulation/payment-chain/{chainId}/regular-currency-exchange";

        public static string ConfirmCurrencyExchange(Guid chainId, Guid operationId) =>
            $"/api/v1/al-musheer-simulation/payment-chain/{chainId}/currency-exchange/{operationId}/confirm";

        public static string GetExchangeDetails(Guid chainId, Guid operationId) =>
            $"/api/v1/al-musheer-simulation/payment-chain/{chainId}/currency-exchange/{operationId}";

        // ========== Average Rate ==========
        public static string AverageRateInfo(Guid walletId) =>
            $"/api/v1/almusheer-average-rate-tracker/wallets/{walletId}/average-rate-info";
    }
}

