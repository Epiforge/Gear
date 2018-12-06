using Gear.Components;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class ActiveUnaryExpression
    {
        [Test]
        public void ConsistentHashCode()
        {
            int hashCode1, hashCode2;
            var john = TestPerson.CreateJohn();
            using (var expr = ActiveExpression.Create(p1 => -p1.Name.Length, john))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create(p1 => -p1.Name.Length, john))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [Test]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create(p1 => -p1.Name.Length, john))
            using (var expr2 = ActiveExpression.Create(p1 => -p1.Name.Length, john))
            using (var expr3 = ActiveExpression.Create(p1 => +p1.Name.Length, john))
            using (var expr4 = ActiveExpression.Create(p1 => -p1.Name.Length, emily))
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
            using (var expr1 = ActiveExpression.Create(p1 => -p1.Name.Length, john))
            using (var expr2 = ActiveExpression.Create(p1 => -p1.Name.Length, john))
            using (var expr3 = ActiveExpression.Create(p1 => +p1.Name.Length, john))
            using (var expr4 = ActiveExpression.Create(p1 => -p1.Name.Length, emily))
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
            using (var expr = ActiveExpression.Create(p1 => -p1.Name.Length, john))
            {
                Assert.IsNull(expr.Fault);
                john.Name = null;
                Assert.IsNotNull(expr.Fault);
                john.Name = "John";
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create(p1 => -p1.Name.Length, john))
            using (var expr2 = ActiveExpression.Create(p1 => -p1.Name.Length, john))
            using (var expr3 = ActiveExpression.Create(p1 => +p1.Name.Length, john))
            using (var expr4 = ActiveExpression.Create(p1 => -p1.Name.Length, emily))
            {
                Assert.IsFalse(expr1 != expr2);
                Assert.IsTrue(expr1 != expr3);
                Assert.IsTrue(expr1 != expr4);
            }
        }

        [Test]
        public void NullableConversion()
        {
            var john = TestPerson.CreateJohn();
            using (var expr = ActiveExpression.Create(p1 => (p1 == null || p1.Name == null ? (int?)null : p1.Name.Length) + 3, john))
            {
                Assert.IsTrue(expr.Value == 7);
                Assert.IsNull(expr.Fault);
                john.Name = null;
                Assert.IsNull(expr.Value);
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void PropertyChanges()
        {
            var john = TestPerson.CreateJohn();
            var values = new BlockingCollection<int>();
            using (var expr = ActiveExpression.Create(p1 => -p1.Name.Length, john))
            {
                var disconnect = expr.OnPropertyChanged(ae => ae.Value, value => values.Add(value));
                values.Add(expr.Value);
                john.Name = "J";
                john.Name = "John";
                john.Name = "Jon";
                john.Name = "Jhn";
                john.Name = string.Empty;
                disconnect();
            }
            Assert.IsTrue(new int[] { -4, -1, -4, -3, 0 }.SequenceEqual(values));
        }

        [Test]
        public void StringConversion()
        {
            var emily = TestPerson.CreateEmily();
            emily.Name = "X";
            using (var expr = ActiveExpression.Create(p1 => -p1.Name.Length, emily))
                Assert.AreEqual("(-{C} /* {X} */.Name /* \"X\" */.Length /* 1 */) /* -1 */", expr.ToString());
        }

        [Test]
        public void ValueAsyncDisposal()
        {
            var people = new ObservableCollection<AsyncDisposableTestPerson>
            {
                AsyncDisposableTestPerson.CreateJohn(),
            };
            AsyncDisposableTestPerson newPerson;
            using (var expr = ActiveExpression.Create(p => -p[0], people))
            {
                newPerson = expr.Value;
                Assert.IsFalse(newPerson.IsDisposed);
                people[0] = AsyncDisposableTestPerson.CreateJohn();
                Assert.IsTrue(newPerson.IsDisposed);
                newPerson = expr.Value;
                Assert.IsFalse(newPerson.IsDisposed);
            }
            Assert.IsTrue(newPerson.IsDisposed);
        }

        [Test]
        public void ValueDisposal()
        {
            var people = new ObservableCollection<SyncDisposableTestPerson>
            {
                SyncDisposableTestPerson.CreateJohn()
            };
            SyncDisposableTestPerson newPerson;
            using (var expr = ActiveExpression.Create(p => -p[0], people))
            {
                newPerson = expr.Value;
                Assert.IsFalse(newPerson.IsDisposed);
                people[0] = SyncDisposableTestPerson.CreateJohn();
                Assert.IsTrue(newPerson.IsDisposed);
                newPerson = expr.Value;
                Assert.IsFalse(newPerson.IsDisposed);
            }
            Assert.IsTrue(newPerson.IsDisposed);
        }
    }
}
