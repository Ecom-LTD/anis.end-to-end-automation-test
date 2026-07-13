namespace Automation.Test.Scenarios
{
    public sealed class Scenario
    {
        private readonly ScenarioContext _ctx;
        private readonly List<(string Name, Func<ScenarioContext, Task> Step)> _steps = new();
        public Scenario(ScenarioContext ctx) => _ctx = ctx;

        public Scenario Step(string name, Func<ScenarioContext, Task> step)
        {
            _steps.Add((name, step));
            return this;
        }

        public async Task RunAsync(Action<string>? log = null)
        {
            foreach (var (name, step) in _steps)
            {
                log?.Invoke($"▶ {name}");
                await step(_ctx);
            }
        }
    }
}
