using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Gear.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveExpressions.MSTest
{
    [TestClass]
    public class Options
    {
        #region Helper Classes

        class TestObject : PropertyChangeNotifier
        {
            AsyncDisposableTestPerson asyncDisposable;
            SyncDisposableTestPerson syncDisposable;

            public SyncDisposableTestPerson GetSyncDisposableMethod() => syncDisposable;

            public AsyncDisposableTestPerson AsyncDisposable
            {
                get => asyncDisposable;
                set => SetBackedProperty(ref asyncDisposable, in value);
            }

            public SyncDisposableTestPerson SyncDisposable
            {
                get => syncDisposable;
                set => SetBackedProperty(ref syncDisposable, in value);
            }
        }

        #endregion Helper Classes

        [TestMethod]
        public void DisposalUnsupported()
        {
            var options = new ActiveExpressionOptions();
            var notSupportedThrown = false;
            var expr = Expression.Lambda<Func<int>>(Expression.Block(Expression.Constant(3)));
            try
            {
                options.AddExpressionValueDisposal(expr);
            }
            catch (NotSupportedException)
            {
                notSupportedThrown = true;
            }
            Assert.IsTrue(notSupportedThrown);
            notSupportedThrown = false;
            try
            {
                options.IsExpressionValueDisposed(expr);
            }
            catch (NotSupportedException)
            {
                notSupportedThrown = true;
            }
            Assert.IsTrue(notSupportedThrown);
            notSupportedThrown = false;
            try
            {
                options.RemoveExpressionValueDisposal(expr);
            }
            catch (NotSupportedException)
            {
                notSupportedThrown = true;
            }
            Assert.IsTrue(notSupportedThrown);
        }

        [TestMethod]
        public void DisposeBinaryResult()
        {
            var options = new ActiveExpressionOptions();
            Assert.IsTrue(options.AddExpressionValueDisposal(() => SyncDisposableTestPerson.CreateJohn() + SyncDisposableTestPerson.CreateEmily()));
            Assert.IsTrue(options.IsExpressionValueDisposed(() => SyncDisposableTestPerson.CreateJohn() + SyncDisposableTestPerson.CreateEmily()));
            Assert.IsTrue(options.RemoveExpressionValueDisposal(() => SyncDisposableTestPerson.CreateJohn() + SyncDisposableTestPerson.CreateEmily()));
        }

        [TestMethod]
        public void DisposeConstructedType()
        {
            var options = new ActiveExpressionOptions();
            Assert.IsTrue(options.AddConstructedTypeDisposal(typeof(SyncDisposableTestPerson)));
            Assert.IsTrue(options.IsConstructedTypeDisposed(typeof(SyncDisposableTestPerson)));
            Assert.IsTrue(options.RemoveConstructedTypeDisposal(typeof(SyncDisposableTestPerson)));
        }

        [TestMethod]
        public void DisposeConstructor()
        {
            var options = new ActiveExpressionOptions();
            Assert.IsTrue(options.AddExpressionValueDisposal(() => new SyncDisposableTestPerson()));
            Assert.IsTrue(options.IsExpressionValueDisposed(() => new SyncDisposableTestPerson()));
            Assert.IsTrue(options.RemoveExpressionValueDisposal(() => new SyncDisposableTestPerson()));
        }

        [TestMethod]
        public void DisposeIndexerValue()
        {
            var collectionType = typeof(ObservableCollection<SyncDisposableTestPerson>);
            var indexer = collectionType.GetProperty("Item");
            var options = new ActiveExpressionOptions();
            Assert.IsTrue(options.AddExpressionValueDisposal(Expression.Lambda<Func<SyncDisposableTestPerson>>(Expression.MakeIndex(Expression.New(collectionType), indexer, new Expression[] { Expression.Constant(0) }))));
            Assert.IsTrue(options.IsExpressionValueDisposed(Expression.Lambda<Func<SyncDisposableTestPerson>>(Expression.MakeIndex(Expression.New(collectionType), indexer, new Expression[] { Expression.Constant(0) }))));
            Assert.IsTrue(options.RemoveExpressionValueDisposal(Expression.Lambda<Func<SyncDisposableTestPerson>>(Expression.MakeIndex(Expression.New(collectionType), indexer, new Expression[] { Expression.Constant(0) }))));
        }

        [TestMethod]
        public void DisposeMethodReturnValue()
        {
            var options = new ActiveExpressionOptions();
            Assert.IsTrue(options.AddExpressionValueDisposal(() => new TestObject().GetSyncDisposableMethod()));
            Assert.IsTrue(options.IsExpressionValueDisposed(() => new TestObject().GetSyncDisposableMethod()));
            Assert.IsTrue(options.RemoveExpressionValueDisposal(() => new TestObject().GetSyncDisposableMethod()));
        }

        [TestMethod]
        public void DisposePropertyValueByExpression()
        {
            var options = new ActiveExpressionOptions();
            Assert.IsTrue(options.AddExpressionValueDisposal(() => new ObservableCollection<SyncDisposableTestPerson>()[0]));
            Assert.IsTrue(options.IsExpressionValueDisposed(() => new ObservableCollection<SyncDisposableTestPerson>()[0]));
            Assert.IsTrue(options.RemoveExpressionValueDisposal(() => new ObservableCollection<SyncDisposableTestPerson>()[0]));
        }

        [TestMethod]
        public void DisposePropertyValueByReflection()
        {
            var testObjectType = typeof(TestObject);
            var property = testObjectType.GetProperty(nameof(TestObject.SyncDisposable));
            var options = new ActiveExpressionOptions();
            Assert.IsTrue(options.AddExpressionValueDisposal(Expression.Lambda<Func<SyncDisposableTestPerson>>(Expression.MakeMemberAccess(Expression.New(testObjectType), property))));
            Assert.IsTrue(options.IsExpressionValueDisposed(Expression.Lambda<Func<SyncDisposableTestPerson>>(Expression.MakeMemberAccess(Expression.New(testObjectType), property))));
            Assert.IsTrue(options.RemoveExpressionValueDisposal(Expression.Lambda<Func<SyncDisposableTestPerson>>(Expression.MakeMemberAccess(Expression.New(testObjectType), property))));
        }

        [TestMethod]
        public void DisposeUnaryResult()
        {
            var options = new ActiveExpressionOptions();
            Assert.IsTrue(options.AddExpressionValueDisposal(() => -SyncDisposableTestPerson.CreateEmily()));
            Assert.IsTrue(options.IsExpressionValueDisposed(() => -SyncDisposableTestPerson.CreateEmily()));
            Assert.IsTrue(options.RemoveExpressionValueDisposal(() => -SyncDisposableTestPerson.CreateEmily()));
        }

        [TestMethod]
        public void OptionsFreeze()
        {
            var invalidThrown = false;
            try
            {
                var options = new ActiveExpressionOptions();
                using (var expr = ActiveExpression.Create(() => true, options))
                    options.DisposeConstructedObjects = false;
            }
            catch (InvalidOperationException)
            {
                invalidThrown = true;
            }
            Assert.IsTrue(invalidThrown);
        }

        // Yes, this test can fail even if nothing is wrong.
        // If you catch it, you win a prize!
        // https://youtu.be/FJ97I9u1i7M
        [TestMethod]
        public void UnfrozenHashCodesDifferent() => Assert.AreNotEqual(new ActiveExpressionOptions().GetHashCode(), new ActiveExpressionOptions().GetHashCode());
    }
}
