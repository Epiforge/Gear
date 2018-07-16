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

        class AsyncDisposable : Components.AsyncDisposable
        {
            protected override async Task DisposeAsync(bool disposing, CancellationToken cancellationToken = default)
            {
                for (var i = 0; i < 10; ++i)
                {
                    await Task.Delay(100);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            public void RequiresNotDisposed() => ThrowIfDisposed();
        }

        class SyncDisposable : Components.SyncDisposable
        {
            protected override void Dispose(bool disposing)
            {
            }

            public void RequiresNotDisposed() => ThrowIfDisposed();
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
    }
}
