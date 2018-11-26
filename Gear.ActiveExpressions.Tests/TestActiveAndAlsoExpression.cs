using NUnit.Framework;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class TestActiveAndAlsoExpression
    {
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
