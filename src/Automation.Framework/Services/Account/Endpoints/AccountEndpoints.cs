namespace Automation.Framework.Services.Account.Endpoints
{
    public static class AccountEndpoints
    {
        public static string ByPhone(string phone) => $"/api/management/v1.0/accounts/filter?Phone={phone}";
    }
}
