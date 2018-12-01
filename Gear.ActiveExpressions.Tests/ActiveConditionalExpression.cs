using Gear.Components;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class ActiveConditionalExpression
    {
        [Test]
        public void FaultShortCircuiting()
        {
            var john = TestPerson.CreateJohn();
            using (var expr = ActiveExpression.Create<TestPerson, TestPerson, string>((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, john, null))
            {
                Assert.AreEqual(john.Name, expr.Value);
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void PropertyChanges()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var values = new BlockingCollection<string>();
            using (var expr = ActiveExpression.Create((p1, p2) => string.IsNullOrEmpty(p1.Name) ? p2.Name : p1.Name, john, emily))
            {
                var disconnect = expr.OnPropertyChanged(ae => ae.Value, value => values.Add(value));
                values.Add(expr.Value);
                john.Name = "J";
                john.Name = "John";
                john.Name = null;
                emily.Name = "E";
                emily.Name = "Emily";
                emily.Name = null;
                emily.Name = "Emily";
                john.Name = "John";
                disconnect();
            }
            Assert.IsTrue(new string[] { "John", "J", "John", "Emily", "E", "Emily", null, "Emily", "John" }.SequenceEqual(values));
        }

        [Test]
        public void ValueShortCircuiting()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, john, emily))
                Assert.AreEqual(john.Name, expr.Value);
            Assert.AreEqual(0, emily.NameGets);
        }
    }
}
