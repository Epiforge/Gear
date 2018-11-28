using NUnit.Framework;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class ActiveOrElseExpression
    {
        [Test]
        public void FaultShortCircuiting()
        {
            var john = TestPerson.CreateJohn();
            using (var expr = ActiveExpression.Create<TestPerson, TestPerson, bool>((p1, p2) => !string.IsNullOrEmpty(p1.Name) || !string.IsNullOrEmpty(p2.Name), john, null))
            {
                Assert.IsTrue(expr.Value);
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void PropertyChanges()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var values = new BlockingCollection<bool>();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name.Length == 1 || p2.Name.Length == 1, john, emily))
            {
                void exprChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(ActiveExpression<TestPerson, TestPerson, bool>.Value))
                        values.Add(expr.Value);
                }
                expr.PropertyChanged += exprChanged;
                values.Add(expr.Value);
                john.Name = "J";
                john.Name = "John";
                emily.Name = "E";
                emily.Name = "Emily";
                john.Name = "J";
                emily.Name = "E";
                john.Name = "John";
                expr.PropertyChanged -= exprChanged;
            }
            Assert.IsTrue(new bool[] { false, true, false, true, false, true }.SequenceEqual(values));
        }

        [Test]
        public void ValueShortCircuiting()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name.Length > 1 || p2.Name.Length > 3, john, emily))
                Assert.IsTrue(expr.Value);
            Assert.AreEqual(0, emily.NameGets);
        }
    }
}
