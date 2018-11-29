using NUnit.Framework;
using System.Collections.Concurrent;
using System.ComponentModel;
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
            using (var expr = ActiveExpression.Create<TestPerson, TestPerson, bool>((p1, p2) => string.IsNullOrEmpty(p1.Name) && string.IsNullOrEmpty(p2.Name), john, null))
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
                void exprChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(ActiveExpression<TestPerson, TestPerson, bool>.Value))
                        values.Add(expr.Value);
                }
                expr.PropertyChanged += exprChanged;
                values.Add(expr.Value);
                john.Name = "J";
                emily.Name = "E";
                john.Name = "John";
                john.Name = "J";
                emily.Name = "Emily";
                emily.Name = "E";
                expr.PropertyChanged -= exprChanged;
            }
            Assert.IsTrue(new bool[] { false, true, false, true, false, true }.SequenceEqual(values));
        }

        [Test]
        public void StringConversion()
        {
            var emily = TestPerson.CreateEmily();
            emily.Name = "E";
            using (var expr = ActiveExpression.Create(p1 => p1.Name == "E" && p1.Name.Length == 1, emily))
                Assert.AreEqual("((p1 /* [TestPerson] */.Name /* \"E\" */ == \"E\") /* True */ && (p1 /* [TestPerson] */.Name /* \"E\" */.Length /* 1 */ == 1) /* True */) /* True */", expr.ToString());
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
