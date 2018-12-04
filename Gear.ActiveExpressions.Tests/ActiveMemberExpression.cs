using Gear.Components;
using NUnit.Framework;
using System;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class ActiveMemberExpression
    {
        #region Test Classes

        class TestObject : PropertyChangeNotifier
        {
            AsyncDisposableTestPerson asyncDisposable;
            SyncDisposableTestPerson syncDisposable;

            public AsyncDisposableTestPerson AsyncDisposable
            {
                get => asyncDisposable;
                set => SetBackedProperty(ref asyncDisposable, value);
            }

            public SyncDisposableTestPerson SyncDisposable
            {
                get => syncDisposable;
                set => SetBackedProperty(ref syncDisposable, value);
            }
        }

        #endregion Test Classes

        [Test]
        public void Closure()
        {
            var x = 3;
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create(p1 => p1.Name == null ? x : emily.Name.Length, john))
            {
                Assert.AreEqual(5, expr.Value);
                john.Name = null;
                Assert.AreEqual(3, expr.Value);
            }
        }

        [Test]
        public void FieldValue()
        {
            var team = (developer: TestPerson.CreateJohn(), artist: TestPerson.CreateEmily());
            using (var expr = ActiveExpression.Create(p1 => p1.artist.Name, team))
                Assert.AreEqual("Emily", expr.Value);
        }

        [Test]
        public void ObjectFaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create((p1, p2) => (p1.Name.Length > 0 ? p1 : p2).Name, john, emily))
            {
                Assert.IsNull(expr.Fault);
                john.Name = null;
                Assert.IsNotNull(expr.Fault);
                john.Name = "John";
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void StaticPropertyValue()
        {
            using (var expr = ActiveExpression.Create(() => Environment.UserName))
                Assert.AreEqual(Environment.UserName, expr.Value);
        }

        [Test]
        public void ValueAsyncDisposal()
        {
            var john = AsyncDisposableTestPerson.CreateJohn();
            var emily = AsyncDisposableTestPerson.CreateEmily();
            var testObject = new TestObject { AsyncDisposable = john };
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => new TestObject().AsyncDisposable);
            using (var expr = ActiveExpression.Create(p1 => p1.AsyncDisposable, testObject, options))
            {
                Assert.AreSame(john, expr.Value);
                Assert.IsFalse(john.IsDisposed);
                testObject.AsyncDisposable = emily;
                Assert.AreSame(emily, expr.Value);
                Assert.IsFalse(emily.IsDisposed);
                Assert.IsTrue(john.IsDisposed);
            }
            Assert.IsTrue(emily.IsDisposed);
        }

        [Test]
        public void ValueDisposal()
        {
            var john = SyncDisposableTestPerson.CreateJohn();
            var emily = SyncDisposableTestPerson.CreateEmily();
            var testObject = new TestObject { SyncDisposable = john };
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => new TestObject().SyncDisposable);
            using (var expr = ActiveExpression.Create(p1 => p1.SyncDisposable, testObject, options))
            {
                Assert.AreSame(john, expr.Value);
                Assert.IsFalse(john.IsDisposed);
                testObject.SyncDisposable = emily;
                Assert.AreSame(emily, expr.Value);
                Assert.IsFalse(emily.IsDisposed);
                Assert.IsTrue(john.IsDisposed);
            }
            Assert.IsTrue(emily.IsDisposed);
        }

        [Test]
        public void ValueDisposalFault()
        {
            var john = SyncDisposableTestPerson.CreateJohn();
            john.ThrowOnDispose = true;
            var testObject = new TestObject { SyncDisposable = john };
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => new TestObject().SyncDisposable);
            using (var expr = ActiveExpression.Create(p1 => p1.SyncDisposable, testObject, options))
            {
                Assert.IsNull(expr.Fault);
                testObject.SyncDisposable = SyncDisposableTestPerson.CreateJohn();
                Assert.IsNotNull(expr.Fault);
                testObject.SyncDisposable = SyncDisposableTestPerson.CreateJohn();
                Assert.IsNull(expr.Fault);
            }
        }
    }
}
