using Gear.Components;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Linq;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class ActiveAndAlsoExpression
    {
        [Test]
        public void ConsistentHashCode()
        {
            int hashCode1, hashCode2;
            var john = TestPerson.CreateJohn();
            using (var expr = ActiveExpression.Create(p1 => p1.Name != null && p1.Name.Length > 0, john))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create(p1 => p1.Name != null && p1.Name.Length > 0, john))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [Test]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create(p1 => p1.Name != null && p1.Name.Length > 0, john))
            using (var expr2 = ActiveExpression.Create(p1 => p1.Name != null && p1.Name.Length > 0, john))
            using (var expr3 = ActiveExpression.Create(p1 => p1.Name == null || p1.Name.Length == 0, john))
            using (var expr4 = ActiveExpression.Create(p1 => p1.Name != null && p1.Name.Length > 0, emily))
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
            using (var expr1 = ActiveExpression.Create(p1 => p1.Name != null && p1.Name.Length > 0, john))
            using (var expr2 = ActiveExpression.Create(p1 => p1.Name != null && p1.Name.Length > 0, john))
            using (var expr3 = ActiveExpression.Create(p1 => p1.Name == null || p1.Name.Length == 0, john))
            using (var expr4 = ActiveExpression.Create(p1 => p1.Name != null && p1.Name.Length > 0, emily))
            {
                Assert.IsTrue(expr1.Equals(expr2));
                Assert.IsFalse(expr1.Equals(expr3));
                Assert.IsFalse(expr1.Equals(expr4));
            }
        }

        [Test]
        public void FaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name.Length > 0 && p2.Name.Length > 0, john, emily))
            {
                Assert.IsNull(expr.Fault);
                john.Name = null;
                Assert.IsNotNull(expr.Fault);
                emily.Name = null;
                john.Name = "John";
                Assert.IsNotNull(expr.Fault);
                emily.Name = "Emily";
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void FaultShortCircuiting()
        {
            var john = TestPerson.CreateJohn();
            TestPerson noOne = null;
            using (var expr = ActiveExpression.Create((p1, p2) => string.IsNullOrEmpty(p1.Name) && string.IsNullOrEmpty(p2.Name), john, noOne))
            {
                Assert.IsFalse(expr.Value);
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create(p1 => p1.Name != null && p1.Name.Length > 0, john))
            using (var expr2 = ActiveExpression.Create(p1 => p1.Name != null && p1.Name.Length > 0, john))
            using (var expr3 = ActiveExpression.Create(p1 => p1.Name == null || p1.Name.Length == 0, john))
            using (var expr4 = ActiveExpression.Create(p1 => p1.Name != null && p1.Name.Length > 0, emily))
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
            var values = new BlockingCollection<bool>();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name.Length == 1 && p2.Name.Length == 1, john, emily))
            {
                var disconnect = expr.OnPropertyChanged(ae => ae.Value, value => values.Add(value));
                values.Add(expr.Value);
                john.Name = "J";
                emily.Name = "E";
                john.Name = "John";
                john.Name = "J";
                emily.Name = "Emily";
                emily.Name = "E";
                disconnect();
            }
            Assert.IsTrue(new bool[] { false, true, false, true, false, true }.SequenceEqual(values));
        }

        [Test]
        public void StringConversion()
        {
            var emily = TestPerson.CreateEmily();
            emily.Name = "X";
            using (var expr = ActiveExpression.Create(p1 => p1.Name == "X" && p1.Name.Length == 1, emily))
                Assert.AreEqual("(({C} /* {X} */.Name /* \"X\" */ == {C} /* \"X\" */) /* True */ && ({C} /* {X} */.Name /* \"X\" */.Length /* 1 */ == {C} /* 1 */) /* True */) /* True */", expr.ToString());
        }

        [Test]
        public void ValueShortCircuiting()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name.Length == 1 && p2.Name.Length > 3, john, emily))
                Assert.IsFalse(expr.Value);
            Assert.Zero(emily.NameGets);
        }
    }
}
