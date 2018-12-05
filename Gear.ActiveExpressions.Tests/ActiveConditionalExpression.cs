using Gear.Components;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Linq;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class ActiveConditionalExpression
    {
        [Test]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, john, emily))
            using (var expr2 = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, john, emily))
            using (var expr3 = ActiveExpression.Create((p1, p2) => p2.Name.Length > 0 ? p2.Name : p1.Name, john, emily))
            using (var expr4 = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, emily, john))
            {
                Assert.IsTrue(expr1 == expr2);
                Assert.IsFalse(expr1 == expr3);
                Assert.IsFalse(expr1 == expr4);
            }
        }

        [Test]
        public void Equals()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, john, emily))
            using (var expr2 = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, john, emily))
            using (var expr3 = ActiveExpression.Create((p1, p2) => p2.Name.Length > 0 ? p2.Name : p1.Name, john, emily))
            using (var expr4 = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, emily, john))
            {
                Assert.IsTrue(expr1.Equals(expr2));
                Assert.IsFalse(expr1.Equals(expr3));
                Assert.IsFalse(expr1.Equals(expr4));
            }
        }

        [Test]
        public void FaultPropagationIfFalse()
        {
            var john = TestPerson.CreateJohn();
            john.Name = null;
            var emily = TestPerson.CreateEmily();
            emily.Name = null;
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name != null ? p1.Name.Length : p2.Name.Length, john, emily))
            {
                Assert.IsNotNull(expr.Fault);
                john.Name = "John";
                Assert.IsNull(expr.Fault);
                john.Name = null;
                Assert.IsNotNull(expr.Fault);
                emily.Name = "Emily";
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void FaultPropagationIfTrue()
        {
            var john = TestPerson.CreateJohn();
            john.Name = null;
            var emily = TestPerson.CreateEmily();
            emily.Name = null;
            using (var expr = ActiveExpression.Create((p1, p2) => p2.Name == null ? p1.Name.Length : p2.Name.Length, john, emily))
            {
                Assert.IsNotNull(expr.Fault);
                emily.Name = "Emily";
                Assert.IsNull(expr.Fault);
                emily.Name = null;
                Assert.IsNotNull(expr.Fault);
                john.Name = "John";
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void FaultPropagationTest()
        {
            var john = TestPerson.CreateJohn();
            john.Name = null;
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, john, emily))
            {
                Assert.IsNotNull(expr.Fault);
                john.Name = "John";
                Assert.IsNull(expr.Fault);
            }
        }

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
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, john, emily))
            using (var expr2 = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, john, emily))
            using (var expr3 = ActiveExpression.Create((p1, p2) => p2.Name.Length > 0 ? p2.Name : p1.Name, john, emily))
            using (var expr4 = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 ? p1.Name : p2.Name, emily, john))
            {
                Assert.IsFalse(expr1 != expr2);
                Assert.IsTrue(expr1 != expr3);
                Assert.IsTrue(expr1 != expr4);
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

        [Test]
        public void StringConversion()
        {
            var john = TestPerson.CreateJohn();
            john.Name = "X";
            var emily = TestPerson.CreateEmily();
            emily.Name = "Y";
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name == null ? p1 : p2, john, emily))
                Assert.AreEqual($"(({{C}} /* {john} */.Name /* \"X\" */ == {{C}} /* null */) /* False */ ? {{C}} /* {john} */ : {{C}} /* {emily} */) /* {emily} */", expr.ToString());
        }
    }
}
