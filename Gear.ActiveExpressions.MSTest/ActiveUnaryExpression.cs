using Gear.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gear.ActiveExpressions.MSTest
{
    [TestClass]
    public class ActiveUnaryExpression
    {
        [TestMethod]
        public void Cast()
        {
            using (var expr = ActiveExpression.Create(p1 => (double)p1, 3))
            {
                Assert.IsNull(expr.Fault);
                Assert.AreEqual(3D, expr.Value);
                Assert.IsInstanceOfType(expr.Value, typeof(double));
            }
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void EvaluationFault()
        {
            TestPerson noOne = null;
            using (var expr = ActiveExpression.Create(() => -noOne))
                Assert.IsNotNull(expr.Fault);
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void StringConversion()
        {
            var emily = TestPerson.CreateEmily();
            emily.Name = "X";
            using (var expr = ActiveExpression.Create(p1 => -p1.Name.Length, emily))
                Assert.AreEqual("(-{C} /* {X} */.Name /* \"X\" */.Length /* 1 */) /* -1 */", expr.ToString());
        }

        [TestMethod]
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

        [TestMethod]
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
