using System;

namespace Gear.Components
{
    /// <summary>
    /// Provides an overridable mechanism for releasing unmanaged resources synchronously
    /// </summary>
    public abstract class OverridableSyncDisposable : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OverridableSyncDisposable"/> class
        /// </summary>
        public OverridableSyncDisposable()
        {
            disposalOverridden = new WeakEventHandler<DisposalNotificationEventArgs>(this);
            disposed = new WeakEventHandler<DisposalNotificationEventArgs>(this);
            disposing = new WeakEventHandler<DisposalNotificationEventArgs>(this);
        }

        /// <summary>
        /// Finalizes this object
        /// </summary>
        ~OverridableSyncDisposable()
        {
            var e = new DisposalNotificationEventArgs(true);
            OnDisposing(e);
            Dispose(false);
            IsDisposed = true;
            OnDisposed(e);
        }

        readonly object disposalAccess = new object();
        readonly WeakEventHandler<DisposalNotificationEventArgs> disposalOverridden;
        readonly WeakEventHandler<DisposalNotificationEventArgs> disposed;
        readonly WeakEventHandler<DisposalNotificationEventArgs> disposing;

        /// <summary>
        /// Occurs when this object's disposal has been overridden
        /// </summary>
        public event EventHandler<DisposalNotificationEventArgs> DisposalOverridden
        {
            add => disposalOverridden.Subscribe(value);
            remove => disposalOverridden.Unsubscribe(value);
        }

        /// <summary>
        /// Occurs when this object has been disposed
        /// </summary>
        public event EventHandler<DisposalNotificationEventArgs> Disposed
        {
            add => disposed.Subscribe(value);
            remove => disposed.Unsubscribe(value);
        }

        /// <summary>
        /// Occurs when this object is being disposed
        /// </summary>
        public event EventHandler<DisposalNotificationEventArgs> Disposing
        {
            add => disposing.Subscribe(value);
            remove => disposing.Unsubscribe(value);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        public void Dispose()
        {
            lock (disposalAccess)
                if (!IsDisposed)
                {
                    var e = new DisposalNotificationEventArgs(false);
                    OnDisposing(e);
                    if (IsDisposed = Dispose(true))
                    {
                        OnDisposed(e);
                        disposalOverridden.Clear();
                        disposing.Clear();
                        disposed.Clear();
                        GC.SuppressFinalize(this);
                    }
                    else
                        OnDisposalOverridden(e);
                }
        }

        /// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
        /// <returns>true if disposal completed; otherwise, false</returns>
        protected abstract bool Dispose(bool disposing);

        /// <summary>
        /// Raises the <see cref="DisposalOverridden"/> event with the specified arguments
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnDisposalOverridden(DisposalNotificationEventArgs e) => disposalOverridden.Raise(e);

        /// <summary>
        /// Raises the <see cref="Disposed"/> event with the specified arguments
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnDisposed(DisposalNotificationEventArgs e) => disposed.Raise(e);

        /// <summary>
        /// Raises the <see cref="Disposing"/> event with the specified arguments
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnDisposing(DisposalNotificationEventArgs e) => disposing.Raise(e);

        /// <summary>
        /// Ensure the object has not been disposed
        /// </summary>
		/// <exception cref="ObjectDisposedException">The object has already been disposed</exception>
		protected void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Gets whether this object has been disposed
        /// </summary>
		public bool IsDisposed { get; private set; }
    }
}
