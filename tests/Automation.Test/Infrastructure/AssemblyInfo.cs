using Xunit;

// التنفيذ متتالٍ: الـ Collections تتشارك محافظ ومنطقة (Tripoli) وحسابات (SulfaOperator_0/1 بنفس الهاتف)،
// فتشغيلها بالتوازي يجعل اختبارين يعدّلان نفس المحفظة/السقف في الوقت نفسه.
// لإعادة التوازي لاحقًا: خصّص مستخدمين ومحافظ منفصلة لكل Collection (UserPool) ثم اجعل
// DisableTestParallelization = false و parallelizeTestCollections = true في xunit.runner.json.
[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]

[assembly: Xunit.TestFramework("Automation.Test.Infrastructure.Reporting.ReportingTestFramework", "Automation.Test")]
