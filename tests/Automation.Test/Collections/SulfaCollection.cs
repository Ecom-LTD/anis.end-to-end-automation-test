using Automation.Test.Fixtures;
using Automation.Test.Fixtures.Fazza;
using Xunit;

namespace Automation.Test.Collections
{
    [CollectionDefinition("Sulfa Collection")]
    public class SulfaCollection : ICollectionFixture<SulfaFixture> { }


    [CollectionDefinition("Almusher Collection")]
    public class AlmusherCollection : ICollectionFixture<AlmuhserFixture> { }
}
