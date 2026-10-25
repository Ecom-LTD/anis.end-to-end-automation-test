using Automation.Test.Fixtures;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Infrastructure;
using Xunit;

namespace Automation.Test.Collections
{
    [CollectionDefinition(CollectionNames.Sulfa)]
    public class Collection : ICollectionFixture<SulfaFixture> { }

    [CollectionDefinition(CollectionNames.Almusher)]
    public class AlmusherCollection : ICollectionFixture<AlmuhserFixture> { }

    [CollectionDefinition(CollectionNames.Fazza)]
    public class FazzaCollection : ICollectionFixture<FazzaFixture> { }

    // مجموعة جديدة: اختبارات الأسقف (AccountLimit, RegionLimit)
    // تعمل بمفردها بلا توازٍ مع أي مجموعة أخرى
    [CollectionDefinition(CollectionNames.RegionLimits, DisableParallelization = true)]
    public class RegionLimitsCollection : ICollectionFixture<SulfaFixture> { }
}