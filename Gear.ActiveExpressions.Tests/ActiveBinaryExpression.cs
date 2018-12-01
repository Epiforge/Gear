using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class ActiveBinaryExpression
    {
        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
        public void ValueDisposalFault()
        {
            var john = SyncDisposableTestPerson.CreateJohn();
            john.ThrowOnDispose = true;
            var people = new ObservableCollection<SyncDisposableTestPerson>
            {
                john,
                SyncDisposableTestPerson.CreateEmily()
            };
            using (var expr = ActiveExpression.Create(p => p[0] + p[1], people))
            {
                Assert.IsNull(expr.Fault);
                people[0] = SyncDisposableTestPerson.CreateJohn();
                Assert.IsNotNull(expr.Fault);
                people[0] = SyncDisposableTestPerson.CreateJohn();
                Assert.IsNull(expr.Fault);
            }
        }
    }
}
