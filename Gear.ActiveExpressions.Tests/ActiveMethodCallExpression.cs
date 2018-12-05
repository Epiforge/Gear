using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class ActiveMethodCallExpression
    {
        #region Test Methods

        AsyncDisposableTestPerson CombineAsyncDisposablePeople(AsyncDisposableTestPerson a, AsyncDisposableTestPerson b) => new AsyncDisposableTestPerson { Name = $"{a.Name} {b.Name}" };

        TestPerson CombinePeople(TestPerson a, TestPerson b) => new TestPerson { Name = $"{a.Name} {b.Name}" };

        SyncDisposableTestPerson CombineSyncDisposablePeople(SyncDisposableTestPerson a, SyncDisposableTestPerson b) => new SyncDisposableTestPerson { Name = $"{a.Name} {b.Name}", ThrowOnDispose = a.ThrowOnDispose || b.ThrowOnDispose };

        TestPerson ReversedCombinePeople(TestPerson a, TestPerson b) => new TestPerson { Name = $"{b.Name} {a.Name}" };

        #endregion Test Methods

        [Test]
        public void ActuallyAProperty()
        {
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create(Expression.Lambda<Func<string>>(Expression.Call(Expression.Constant(emily), typeof(TestPerson).GetRuntimeProperty(nameof(TestPerson.Name)).GetMethod))))
            {
                Assert.IsNull(expr.Fault);
                Assert.AreEqual("Emily", expr.Value);
                emily.Name = "E";
                Assert.IsNull(expr.Fault);
                Assert.AreEqual("E", expr.Value);
            }
        }

        [Test]
        public void ArgumentFaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create(() => CombinePeople(john.Name.Length > 3 ? john : null, emily)))
            {
                Assert.IsNull(expr.Fault);
                john.Name = null;
                Assert.IsNotNull(expr.Fault);
                john.Name = "John";
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily))
            using (var expr2 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily))
            using (var expr3 = ActiveExpression.Create((p1, p2) => ReversedCombinePeople(p1, p2), john, emily))
            using (var expr4 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), emily, john))
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
            using (var expr1 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily))
            using (var expr2 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily))
            using (var expr3 = ActiveExpression.Create((p1, p2) => ReversedCombinePeople(p1, p2), john, emily))
            using (var expr4 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), emily, john))
            {
                Assert.IsTrue(expr1.Equals(expr2));
                Assert.IsFalse(expr1.Equals(expr3));
                Assert.IsFalse(expr1.Equals(expr4));
            }
        }

        [Test]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily))
            using (var expr2 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily))
            using (var expr3 = ActiveExpression.Create((p1, p2) => ReversedCombinePeople(p1, p2), john, emily))
            using (var expr4 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), emily, john))
            {
                Assert.IsFalse(expr1 != expr2);
                Assert.IsTrue(expr1 != expr3);
                Assert.IsTrue(expr1 != expr4);
            }
        }

        [Test]
        public void ObjectFaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create(() => (john.Name.Length > 3 ? this : null).CombinePeople(john, emily)))
            {
                Assert.IsNull(expr.Fault);
                john.Name = null;
                Assert.IsNotNull(expr.Fault);
                john.Name = "John";
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void StringConversion()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily))
                Assert.AreEqual($"{{C}} /* {this} */.CombinePeople({{C}} /* {john} */, {{C}} /* {emily} */) /* {expr.Value} */", expr.ToString());
        }

        [Test]
        public void ValueAsyncDisposal()
        {
            var john = AsyncDisposableTestPerson.CreateJohn();
            var emily = AsyncDisposableTestPerson.CreateEmily();
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => CombineAsyncDisposablePeople(null, null));
            AsyncDisposableTestPerson first, second;
            using (var expr = ActiveExpression.Create(() => CombineAsyncDisposablePeople(john.Name.Length > 3 ? john : emily, emily), options))
            {
                Assert.IsNull(expr.Fault);
                first = expr.Value;
                Assert.IsFalse(first.IsDisposed);
                john.Name = string.Empty;
                Assert.IsNull(expr.Fault);
                second = expr.Value;
                Assert.IsFalse(second.IsDisposed);
                Assert.IsTrue(first.IsDisposed);
            }
            Assert.IsTrue(second.IsDisposed);
        }

        [Test]
        public void ValueDisposal()
        {
            var john = SyncDisposableTestPerson.CreateJohn();
            var emily = SyncDisposableTestPerson.CreateEmily();
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => CombineSyncDisposablePeople(null, null));
            SyncDisposableTestPerson first, second;
            using (var expr = ActiveExpression.Create(() => CombineSyncDisposablePeople(john.Name.Length > 3 ? john : emily, emily), options))
            {
                Assert.IsNull(expr.Fault);
                first = expr.Value;
                Assert.IsFalse(first.IsDisposed);
                john.Name = string.Empty;
                Assert.IsNull(expr.Fault);
                second = expr.Value;
                Assert.IsFalse(second.IsDisposed);
                Assert.IsTrue(first.IsDisposed);
            }
            Assert.IsTrue(second.IsDisposed);
        }

        [Test]
        public void ValueDisposalFault()
        {
            var john = SyncDisposableTestPerson.CreateJohn();
            john.ThrowOnDispose = true;
            var people = new ObservableCollection<SyncDisposableTestPerson> { john };
            var emily = SyncDisposableTestPerson.CreateEmily();
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => CombineSyncDisposablePeople(null, null));
            using (var expr = ActiveExpression.Create(() => CombineSyncDisposablePeople(people[0], emily), options))
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
