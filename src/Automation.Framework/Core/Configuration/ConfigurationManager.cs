using Microsoft.Extensions.Configuration;

namespace Automation.Framework.Configuration
{
    public static class ConfigurationManager
    {
        private static readonly Lazy<TestSettings> _settings = new(Load);

        /// <summary>تحميل كسول: أي فشل يظهر بالاستثناء الأصلي (وليس TypeInitializationException).</summary>
        public static TestSettings Settings => _settings.Value;

        private static TestSettings Load()
        {
            // الأولوية: appsettings.json ثم appsettings.{TEST_ENV}.json (اختياري) ثم Environment Variables
            // (مثل Users__0__Code لإبقاء الأسرار خارج الملف).
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("Configuration/appsettings.json", optional: false)
                .AddJsonFile($"Configuration/appsettings.{Environment.GetEnvironmentVariable("TEST_ENV")}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var settings = config.Get<TestSettings>()
                ?? throw new InvalidOperationException("Failed to load appsettings.json");

            var duplicates = settings.Users.GroupBy(u => u.Key).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Count > 0)
                throw new InvalidOperationException($"Duplicate user keys in appsettings.json: {string.Join(", ", duplicates)}");

            return settings;
        }
    }
}
