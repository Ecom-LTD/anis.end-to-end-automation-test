namespace Automation.Framework.Helpers
{
    /// <summary>فحوصات مشتركة لبيانات الجلسات (تُستخدم في كل الـ Readiness Flows).</summary>
    public static class Guard
    {
        public static void RequiredValue(string? value, string propertyName, string sessionName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"{propertyName} is missing from {sessionName}.");
        }

        public static void RequiredGuid(string? value, string propertyName, string sessionName)
        {
            if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty)
                throw new InvalidOperationException(
                    $"{propertyName} in {sessionName} is not a valid Guid. Current value: '{value}'.");
        }
    }
}