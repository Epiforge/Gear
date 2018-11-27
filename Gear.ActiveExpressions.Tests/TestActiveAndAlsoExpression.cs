using NUnit.Framework;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class TestActiveAndAlsoExpression
    {
        [Test]
        public void ShortCircuitsFault()
        {
            var john = TestPerson.CreateJohn();
            using (var expr = ActiveExpression.Create<TestPerson, TestPerson, bool>((p1, p2) => string.IsNullOrEmpty(p1.Name) && string.IsNullOrEmpty(p2.Name), john, null))
            {
                Assert.IsFalse(expr.Value);
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void ShortCircuitsValue()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name.Length == 1 && p2.Name.Length > 3, john, emily))
                Assert.IsFalse(expr.Value);
            Assert.AreEqual(0, emily.NameGets);
        }
    }
}
