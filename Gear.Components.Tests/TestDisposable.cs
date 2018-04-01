using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components.Tests
{
    [TestFixture]
    class TestDisposable
    {
        #region Helper Classes

        class AsyncDisposable : Disposable
        {
            protected override async Task DisposeAsync(bool disposing, CancellationToken cancellationToken = default(CancellationToken))
            {
                for (var i = 0; i < 10; ++i)
                {
                    await Task.Delay(100);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            public void RequiresNotDisposed() => ThrowIfDisposed();

            protected override bool IsAsyncDisposable => true;

            new public bool IsDisposed => base.IsDisposed;
        }

        class IncompleteImplementationDisposable : Disposable
        {
            protected override bool IsAsyncDisposable => true;

            protected override bool IsDisposable => true;

            new public bool IsDisposed => base.IsDisposed;
        }

        class SyncDisposable : Disposable
        {
            protected override void Dispose(bool disposing)
            {
            }

            public void RequiresNotDisposed() => ThrowIfDisposed();

            protected override bool IsDisposable => true;

            new public bool IsDisposed => base.IsDisposed;
        }

        class UnimplementedDisposable : Disposable
        {
            new public bool IsDisposed => base.IsDisposed;
        }

        #endregion Helper Classes

        [Test]
        public async Task AsyncDisposableDisposesAsync()
        {
            var disposable = new AsyncDisposable();
            Assert.IsFalse(disposable.IsDisposed);
            await disposable.UsingAsync(() => { });
            Assert.IsTrue(disposable.IsDisposed);
        }

        [Test]
        public async Task AsyncDisposableCancelsDisposeAsync()
        {
            var disposable = new AsyncDisposable();
            Assert.IsFalse(disposable.IsDisposed);
            var threwOperationCancelled = false;
            try
            {
                using (var cts = new CancellationTokenSource(500))
                    await disposable.UsingAsync(() => { }, cts.Token);
            }
            catch (OperationCanceledException)
            {
                threwOperationCancelled = true;
            }
            Assert.IsFalse(disposable.IsDisposed);
            Assert.IsTrue(threwOperationCancelled);
        }

        [Test]
        public async Task AsyncDisposableThrowsOnceDisposedAsync()
        {
            var disposable = new AsyncDisposable();
            await disposable.UsingAsync(() => { });
            var threwObjectDisposed = false;
            try
            {
                disposable.RequiresNotDisposed();
            }
            catch (ObjectDisposedException)
            {
                threwObjectDisposed = true;
            }
            Assert.IsTrue(threwObjectDisposed);
        }

        [Test]
        public void IncompleteImplementationDisposableDoesNotDispose()
        {
            var disposable = new IncompleteImplementationDisposable();
            Assert.IsFalse(disposable.IsDisposed);
            var threwNotImplemented = false;
            try
            {
                using (disposable)
                {
                }
            }
            catch (NotImplementedException)
            {
                threwNotImplemented = true;
            }
            Assert.IsTrue(threwNotImplemented);
            Assert.IsFalse(disposable.IsDisposed);
        }

        [Test]
        public async Task IncompleteImplementationDisposableDoesNotDisposeAsync()
        {
            var disposable = new IncompleteImplementationDisposable();
            Assert.IsFalse(disposable.IsDisposed);
            var threwNotImplemented = false;
            try
            {
                await disposable.UsingAsync(() => { });
            }
            catch (NotImplementedException)
            {
                threwNotImplemented = true;
            }
            Assert.IsTrue(threwNotImplemented);
            Assert.IsFalse(disposable.IsDisposed);
        }

        [Test]
        public void SyncDisposableDisposes()
        {
            var disposable = new SyncDisposable();
            Assert.IsFalse(disposable.IsDisposed);
            using (disposable)
            {
            }
            Assert.IsTrue(disposable.IsDisposed);
        }

        [Test]
        public void SyncDisposableThrowsOnceDisposed()
        {
            var disposable = new SyncDisposable();
            using (disposable)
            {
            }
            var threwObjectDisposed = false;
            try
            {
                disposable.RequiresNotDisposed();
            }
            catch (ObjectDisposedException)
            {
                threwObjectDisposed = true;
            }
            Assert.IsTrue(threwObjectDisposed);
        }

        [Test]
        public void UnimplementedDisposableDoesNotDispose()
        {
            var disposable = new UnimplementedDisposable();
            Assert.IsFalse(disposable.IsDisposed);
            var threwInvalidOperation = false;
            try
            {
                using (disposable)
                {
                }
            }
            catch (InvalidOperationException)
            {
                threwInvalidOperation = true;
            }
            Assert.IsTrue(threwInvalidOperation);
            Assert.IsFalse(disposable.IsDisposed);
        }

        [Test]
        public async Task UnimplementedDisposableDoesNotDisposeAsync()
        {
            var disposable = new UnimplementedDisposable();
            Assert.IsFalse(disposable.IsDisposed);
            var threwInvalidOperation = false;
            try
            {
                await disposable.UsingAsync(() => { });
            }
            catch (InvalidOperationException)
            {
                threwInvalidOperation = true;
            }
            Assert.IsTrue(threwInvalidOperation);
            Assert.IsFalse(disposable.IsDisposed);
        }
    }
}
