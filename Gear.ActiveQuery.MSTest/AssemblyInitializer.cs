using Gear.ActiveExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveQuery.MSTest
{
    [TestClass]
    public class AssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context) => ActiveExpression.Optimizer = ExpressionOptimizer.tryVisit;
    }
}
