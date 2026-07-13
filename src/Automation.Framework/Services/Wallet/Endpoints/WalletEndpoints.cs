namespace Automation.Framework.Services.Wallet.Endpoints
{
    public static class WalletEndpoints
    {
        //Consumer Endpoints
        public const string Profile = "/api/consumers/v1.0/profile?code=2";
        public static string Balance(System.Guid walletId) => $"/api/consumers/v1.0/transactions/{walletId}/current-balance";
        public static string AllWallet(string phone) =>
           $"/api/management/v1.0/wallets/load?value={phone}";
        public static string UpdateDefaultWallet = "/api/consumers/v1.0/profile/update-default-wallet";
        public static string CreateWallet = "/api/consumers/v1.0/wallets/create";
        
    }
}
