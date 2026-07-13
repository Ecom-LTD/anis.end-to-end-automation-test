namespace Automation.Framework.Configuration
{
    public class TestSettings
    {
        public ApiSettings ApiSettings { get; set; } = new();
        public List<UserCredentials> Users { get; set; } = new();
    }

    public class ApiSettings
    {
        public string GatewayUrl { get; set; } = string.Empty;
        public string IdentityUrl { get; set; } = string.Empty;
        public string FinOpsManagementUrl { get; set; } = string.Empty;
        public string FinOpsConsumersUrl { get; set; } = string.Empty;
        public string FalconsUrl { get; set; } = string.Empty;
        public string AnisPaymentsUrl { get; set; } = string.Empty;

        /// <summary>عند true تُستخدم طبقة API وهمية بدل الشبكة (للتشغيل دون خادم حقيقي).</summary>
        public bool UseFakeBackend { get; set; } = false;
    }

    public class UserCredentials
    {
        public string Key { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Project { get; set; } = "Shared";
        public string PhoneNumber { get; set; } = string.Empty;
        public string SubscriptionName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
