using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveQuery.Tests
{
    [TestClass]
    public class AssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context) => ActiveQueryOptions.Optimizer = ExpressionOptimizer.tryVisit;
    }
}
