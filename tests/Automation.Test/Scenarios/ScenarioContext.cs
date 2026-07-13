using Microsoft.Extensions.DependencyInjection;

namespace Automation.Test.Scenarios
{
    public sealed class ScenarioContext
    {
        private readonly Dictionary<string, object?> _bag = new();
        public IServiceProvider Services { get; }
        public ScenarioContext(IServiceProvider services) => Services = services;

        public TFlow Flow<TFlow>() where TFlow : notnull => Services.GetRequiredService<TFlow>();
        public ScenarioContext Set(string key, object? value) { _bag[key] = value; return this; }
        public T Get<T>(string key) => (T)_bag[key]!;
    }
}
