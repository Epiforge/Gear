using Gear.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gear.ActiveExpressions.MSTest
{
    [TestClass]
    public class ActiveBinaryExpression
    {
        [TestMethod]
        public void ConsistentHashCode()
        {
            int hashCode1, hashCode2;
            var john = TestPerson.CreateJohn();
            using (var expr = ActiveExpression.Create(p1 => p1.Name.Length + 2, john))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create(p1 => p1.Name.Length + 2, john))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [TestMethod]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create(p1 => p1.Name.Length + 2, john))
            using (var expr2 = ActiveExpression.Create(p1 => p1.Name.Length + 2, john))
            using (var expr3 = ActiveExpression.Create(p1 => p1.Name.Length - 2, john))
            using (var expr4 = ActiveExpression.Create(p1 => p1.Name.Length + 2, emily))
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
            using (var expr1 = ActiveExpression.Create(p1 => p1.Name.Length + 2, john))
            using (var expr2 = ActiveExpression.Create(p1 => p1.Name.Length + 2, john))
            using (var expr3 = ActiveExpression.Create(p1 => p1.Name.Length - 2, john))
            using (var expr4 = ActiveExpression.Create(p1 => p1.Name.Length + 2, emily))
            {
                Assert.IsTrue(expr1.Equals(expr2));
                Assert.IsFalse(expr1.Equals(expr3));
                Assert.IsFalse(expr1.Equals(expr4));
            }
        }

        [TestMethod]
        public void FaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name.Length + p2.Name.Length, john, emily))
            {
                Assert.IsNull(expr.Fault);
                john.Name = null;
                Assert.IsNotNull(expr.Fault);
                emily.Name = null;
                john.Name = string.Empty;
                Assert.IsNotNull(expr.Fault);
                emily.Name = "Emily";
                Assert.IsNull(expr.Fault);
            }
        }

        [TestMethod]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create(p1 => p1.Name.Length + 2, john))
            using (var expr2 = ActiveExpression.Create(p1 => p1.Name.Length + 2, john))
            using (var expr3 = ActiveExpression.Create(p1 => p1.Name.Length - 2, john))
            using (var expr4 = ActiveExpression.Create(p1 => p1.Name.Length + 2, emily))
            {
                Assert.IsFalse(expr1 != expr2);
                Assert.IsTrue(expr1 != expr3);
                Assert.IsTrue(expr1 != expr4);
            }
        }

        [TestMethod]
        public void PropertyChanges()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var values = new BlockingCollection<int>();
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name.Length + p2.Name.Length, john, emily))
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
            Assert.IsTrue(new int[] { 9, 6, 2, 5, 2, 6, 2 }.SequenceEqual(values));
        }

        [TestMethod]
        public void StringConversion()
        {
            var emily = TestPerson.CreateEmily();
            emily.Name = "X";
            using (var expr = ActiveExpression.Create(p1 => p1.Name.Length + 1, emily))
                Assert.AreEqual("({C} /* {X} */.Name /* \"X\" */.Length /* 1 */ + {C} /* 1 */) /* 2 */", expr.ToString());
        }

        [TestMethod]
        public void ValueAsyncDisposal()
        {
            var people = new ObservableCollection<AsyncDisposableTestPerson>
            {
                AsyncDisposableTestPerson.CreateJohn(),
                AsyncDisposableTestPerson.CreateEmily()
            };
            AsyncDisposableTestPerson newPerson;
            using (var expr = ActiveExpression.Create(p => p[0] + p[1], people))
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
                SyncDisposableTestPerson.CreateJohn(),
                SyncDisposableTestPerson.CreateEmily()
            };
            SyncDisposableTestPerson newPerson;
            using (var expr = ActiveExpression.Create(p => p[0] + p[1], people))
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
