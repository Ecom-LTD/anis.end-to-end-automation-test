using Microsoft.Extensions.Configuration;

namespace Automation.Framework.Configuration
{
    public static class ConfigurationManager
    {
        public static TestSettings Settings { get; }

        static ConfigurationManager()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("Configuration/appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            Settings = config.Get<TestSettings>()
                ?? throw new Exception("Failed to load appsettings.json");
        }
    }
}
