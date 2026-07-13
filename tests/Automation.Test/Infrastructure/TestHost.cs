using Automation.Framework.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace Automation.Test.Infrastructure
{
    /// <summary>حاوية DI واحدة لكل عملية اختبار، يشاركها كل الـ Fixtures والـ Collections.</summary>
    public static class TestHost
    {
        private static readonly Lazy<IServiceProvider> _provider = new(() =>
            new ServiceCollection().AddAutomationFramework().BuildServiceProvider(validateScopes: false));

        public static IServiceProvider Services => _provider.Value;
        public static T Resolve<T>() where T : notnull => Services.GetRequiredService<T>();
    }
}
