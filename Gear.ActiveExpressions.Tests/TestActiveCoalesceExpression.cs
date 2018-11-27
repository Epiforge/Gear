using NUnit.Framework;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class TestActiveCoalesceExpression
    {
        [Test]
        public void ShortCircuitsFault()
        {
            var john = TestPerson.CreateJohn();
            using (var expr = ActiveExpression.Create<TestPerson, TestPerson, string>((p1, p2) => p1.Name ?? p2.Name, john, null))
            {
                Assert.AreEqual(john.Name, expr.Value);
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void ShortCircuitsValue()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name ?? p2.Name, john, emily))
                Assert.AreEqual(john.Name, expr.Value);
            Assert.AreEqual(0, emily.NameGets);
        }

        [Test]
        public void SupportsChanges()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var values = new BlockingCollection<string>();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name ?? p2.Name, john, emily))
            {
                void exprChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(ActiveExpression<TestPerson, TestPerson, string>.Value))
                        values.Add(expr.Value);
                }
                expr.PropertyChanged += exprChanged;
                values.Add(expr.Value);
                john.Name = "J";
                john.Name = "John";
                john.Name = null;
                emily.Name = "E";
                emily.Name = "Emily";
                emily.Name = null;
                emily.Name = "Emily";
                john.Name = "John";
                expr.PropertyChanged -= exprChanged;
            }
            Assert.IsTrue(new string[] { "John", "J", "John", "Emily", "E", "Emily", null, "Emily", "John" }.SequenceEqual(values));
        }
    }
}
