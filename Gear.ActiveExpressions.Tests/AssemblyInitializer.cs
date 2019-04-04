using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveExpressions.Tests
{
    [TestClass]
    public class AssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context) => ActiveExpression.Optimizer = ExpressionOptimizer.tryVisit;
    }
}
