namespace Automation.Framework.Services.FazzaTopup.Models
{
    public sealed class FazzaLimitReadinessOptions
    {
        public decimal RequiredOperatorAvailableLimit { get; init; }
            = 1000m;

        public decimal RequiredBusinessAvailableLimit { get; init; }
            = 1000m;

        public bool AutoIncreaseInsufficientLimits { get; init; }
            = true;

        public decimal LimitSafetyMargin { get; init; }
            = 100m;

        public int VerificationAttempts { get; init; }
            = 6;

        public TimeSpan VerificationDelay { get; init; }
            = TimeSpan.FromMilliseconds(500);
    }

    public sealed class FazzaLimitReadinessResult
    {
        public decimal OperatorAvailableLimit { get; init; }

        public decimal BusinessAvailableLimit { get; init; }

        public bool OperatorLimitWasUpdated { get; init; }

        public bool BusinessLimitWasUpdated { get; init; }
    }
}