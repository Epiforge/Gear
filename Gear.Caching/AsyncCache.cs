using Gear.Components;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Caching
{
    /// <summary>
    /// A cache that intelligently manages expiring and refreshing values
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the cache</typeparam>
    /// <typeparam name="TValue">The base type of the values in the cache</typeparam>
    public partial class AsyncCache<TKey, TValue> : Disposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCache{TKey, TValue}"/> class
        /// </summary>
        /// <param name="operationTimeout">The maximum amount of time any operation on the cache may take before failing (if <c>null</c> or <c>default</c>, operations may be attempted for an infinite period of time)</param>
        /// <param name="weakenReferencesIn">The amount of time a value may remain unaccessed before potentially being subjected to garbage collection (if not specified, values will never be garbage collected)</param>
        /// <param name="addReferencesAsWeak">Whether newly added values start as weak references (has no effect if <paramref name="weakenReferencesIn"/> is <c>null</c> or <c>default</c>)</param>
        public AsyncCache(TimeSpan? operationTimeout = default, TimeSpan? weakenReferencesIn = default, bool addReferencesAsWeak = false)
        {
            OperationTimeout = operationTimeout;
            AddReferencesAsWeak = addReferencesAsWeak;
            buckets = new ConcurrentDictionary<TKey, Bucket>();
            refreshCancellationTokenSources = new ConcurrentDictionary<TKey, CancellationTokenSource>();
            refreshAutoResetEvents = new ConcurrentDictionary<TKey, AsyncAutoResetEvent>();
            retrievalAccess = new ConcurrentDictionary<TKey, AsyncReaderWriterLock>();
            WeakenReferencesIn = weakenReferencesIn;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCache{TKey, TValue}"/> class that uses the specified <see cref="IEqualityComparer{T}"/>
        /// </summary>
        /// <param name="comparer">The equality comparison implementation to use when comparing keys</param>
        /// <param name="operationTimeout">The maximum amount of time any operation on the cache may take before failing (if <c>null</c> or <c>default</c>, operations may be attempted for an infinite period of time)</param>
        /// <param name="weakenReferencesIn">The amount of time a value may remain unaccessed before potentially being subjected to garbage collection (if not specified, values will never be garbage collected)</param>
        /// <param name="addReferencesAsWeak">Whether newly added values start as weak references (has no effect if <paramref name="weakenReferencesIn"/> is <c>null</c> or <c>default</c>)</param>
        public AsyncCache(IEqualityComparer<TKey> comparer, TimeSpan? operationTimeout = default, TimeSpan? weakenReferencesIn = default, bool addReferencesAsWeak = false)
        {
            OperationTimeout = operationTimeout;
            AddReferencesAsWeak = addReferencesAsWeak;
            this.comparer = comparer;
            buckets = new ConcurrentDictionary<TKey, Bucket>(comparer);
            refreshCancellationTokenSources = new ConcurrentDictionary<TKey, CancellationTokenSource>(comparer);
            refreshAutoResetEvents = new ConcurrentDictionary<TKey, AsyncAutoResetEvent>(comparer);
            retrievalAccess = new ConcurrentDictionary<TKey, AsyncReaderWriterLock>(comparer);
            WeakenReferencesIn = weakenReferencesIn;
        }

        ConcurrentDictionary<TKey, Bucket> buckets;
        readonly IEqualityComparer<TKey> comparer;
        readonly ConcurrentDictionary<TKey, CancellationTokenSource> refreshCancellationTokenSources;
        readonly ConcurrentDictionary<TKey, AsyncAutoResetEvent> refreshAutoResetEvents;
        ConcurrentDictionary<TKey, AsyncReaderWriterLock> retrievalAccess;

        /// <summary>
        /// Gets whether newly added values start as weak references (has no effect if <see cref="WeakenReferencesIn"/> is <c>null</c> or <c>default</c>)
        /// </summary>
        public bool AddReferencesAsWeak { get; }

        /// <summary>
        /// Gets the maximum amount of time any operation on the cache may take before failing (if <c>null</c> or <c>default</c>, operations may be attempted for an infinite period of time)
        /// </summary>
        public TimeSpan? OperationTimeout { get; }

        /// <summary>
        /// Gets the amount of time a value can be left unaccessed by callers before being potentially subjected to garbage collection (if <c>null</c> or <c>default</c>, values will never be garbage collected)
        /// </summary>
        public TimeSpan? WeakenReferencesIn { get; }

        /// <summary>
        /// Occurs after the cache is reset
        /// </summary>
        public event EventHandler AfterReset;

        /// <summary>
        /// Occurs before the cache is reset
        /// </summary>
        public event EventHandler BeforeReset;

        /// <summary>
        /// Occurs when a value is added to the cache
        /// </summary>
        public event EventHandler<KeyValueEventArgs> ValueAdded;

        /// <summary>
        /// Occurs when an expired value is discovered in the cache
        /// </summary>
        /// <remarks>
        /// The cache does not proactively scan for expired values to evict.
        /// This event is only raised when a caller causes the cache to retrieve the value and it is found to be expired.
        /// </remarks>
        public event EventHandler<ValueExpiredEventArgs> ValueExpired;

        /// <summary>
        /// Occurs when an attempt to add a value to the cache fails
        /// </summary>
        public event EventHandler<KeyedExceptionEventArgs> ValueAddFailed;

        /// <summary>
        /// Occurs when a value's reference was weakened, it was garbage collected, and then accessed by a caller, causing it to be reconstructed
        /// </summary>
        public event EventHandler<KeyValueEventArgs> ValueReconstructed;

        /// <summary>
        /// Occurs when a value's reference has been weakened because it hasn't been accessed in the amount of time specified by <see cref="WeakenReferencesIn"/>
        /// </summary>
        public event EventHandler<KeyValueEventArgs> ValueReferenceWeakened;

        /// <summary>
        /// Occurs when an attempt to refresh a value in the cache fails
        /// </summary>
        public event EventHandler<KeyedExceptionEventArgs> ValueRefreshFailed;

        /// <summary>
        /// Occurs when a value is removed from the cache
        /// </summary>
        public event EventHandler<KeyValueEventArgs> ValueRemoved;

        /// <summary>
        /// Occurs when a value in the cache is updated
        /// </summary>
        public event EventHandler<ValueUpdatedEventArgs> ValueUpdated;

        /// <summary>
        /// Occurs when an attempt to update a value in the cache fails
        /// </summary>
        public event EventHandler<KeyedExceptionEventArgs> ValueUpdateFailed;

        /// <summary>
        /// Adds a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void Add(TKey key, TValue value, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = PerformTryAdd(key, ValueSource<TValue>.Create(value), expireIn, cancellationToken);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(value), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void Add(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = PerformTryAdd(key, ValueSource<TValue>.Create(valueFactory), expireIn, cancellationToken);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(valueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void Add(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = PerformTryAdd(key, ValueSource<TValue>.Create(cancelableValueFactory), expireIn, cancellationToken);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task AddAsync(TKey key, TValue value, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await PerformTryAddAsync(key, ValueSource<TValue>.Create(value), expireIn, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(value), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task AddAsync(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await PerformTryAddAsync(key, ValueSource<TValue>.Create(valueFactory), expireIn, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(valueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task AddAsync(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await PerformTryAddAsync(key, ValueSource<TValue>.Create(cancelableValueFactory), expireIn, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task AddAsync(TKey key, Func<Task<TValue>> asyncValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await PerformTryAddAsync(key, ValueSource<TValue>.Create(asyncValueFactory), expireIn, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(asyncValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task AddAsync(TKey key, Func<CancellationToken, Task<TValue>> cancelableAsyncValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await PerformTryAddAsync(key, ValueSource<TValue>.Create(cancelableAsyncValueFactory), expireIn, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableAsyncValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void AddAndRefresh(TKey key, Func<TValue> valueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = PerformTryAddAndRefresh(key, ValueSource<TValue>.Create(valueFactory), interval, cancellationToken);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(valueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void AddAndRefresh(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = PerformTryAddAndRefresh(key, ValueSource<TValue>.Create(cancelableValueFactory), interval, cancellationToken);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task AddAndRefreshAsync(TKey key, Func<TValue> valueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await PerformTryAddAndRefreshAsync(key, ValueSource<TValue>.Create(valueFactory), interval, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(valueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task AddAndRefreshAsync(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await PerformTryAddAndRefreshAsync(key, ValueSource<TValue>.Create(cancelableValueFactory), interval, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task AddAndRefreshAsync(TKey key, Func<Task<TValue>> asyncValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await PerformTryAddAndRefreshAsync(key, ValueSource<TValue>.Create(asyncValueFactory), interval, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(asyncValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to the cache and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="ArgumentException">A value with the same <paramref name="key"/> has already been added</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task AddAndRefreshAsync(TKey key, Func<CancellationToken, Task<TValue>> cancelableAsyncValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await PerformTryAddAndRefreshAsync(key, ValueSource<TValue>.Create(cancelableAsyncValueFactory), interval, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.DuplicateKey:
                    throw new ArgumentException("A value with the same key has already been added", nameof(key));
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableAsyncValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Adds a value to or updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void AddOrUpdate(TKey key, TValue value, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            PerformAddOrUpdate(key, ValueSource<TValue>.Create(value), expireIn, cancellationToken);
        }

        /// <summary>
        /// Adds a value to or updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void AddOrUpdate(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            PerformAddOrUpdate(key, ValueSource<TValue>.Create(valueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Adds a value to or updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void AddOrUpdate(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            PerformAddOrUpdate(key, ValueSource<TValue>.Create(cancelableValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Adds a value to or updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task AddOrUpdateAsync(TKey key, TValue value, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformAddOrUpdateAsync(key, ValueSource<TValue>.Create(value), expireIn, cancellationToken);
        }

        /// <summary>
        /// Adds a value to or updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task AddOrUpdateAsync(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformAddOrUpdateAsync(key, ValueSource<TValue>.Create(valueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Adds a value to or updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task AddOrUpdateAsync(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformAddOrUpdateAsync(key, ValueSource<TValue>.Create(cancelableValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Adds a value to or updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task AddOrUpdateAsync(TKey key, Func<Task<TValue>> asyncValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformAddOrUpdateAsync(key, ValueSource<TValue>.Create(asyncValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Adds a value to or updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task AddOrUpdateAsync(TKey key, Func<CancellationToken, Task<TValue>> cancelableAsyncValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformAddOrUpdateAsync(key, ValueSource<TValue>.Create(cancelableAsyncValueFactory), expireIn, cancellationToken);
        }

        static AsyncAutoResetEvent AutoResetEventFactory(TKey key) => new AsyncAutoResetEvent();

        static AsyncAutoResetEvent AutoResetEventFactory(TKey key, AsyncAutoResetEvent previousAutoResetEvent) => new AsyncAutoResetEvent();

        static CancellationTokenSource CancellationTokenSourceFactory(TKey key) => new CancellationTokenSource();

        static CancellationTokenSource CancellationTokenSourceFactory(TKey key, CancellationTokenSource previousCancellationTokenSource)
        {
            Task.Run(() =>
            {
                try
                {
                    previousCancellationTokenSource.Cancel();
                    previousCancellationTokenSource.Dispose();
                }
                catch
                {
                    // Allow this to happen silently
                }
            });
            return new CancellationTokenSource();
        }

        /// <summary>
        /// Cancels refreshing a value
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void CancelRefreshing(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!TryCancelRefreshing(key, cancellationToken))
                throw new KeyNotFoundException();
        }

        /// <summary>
        /// Cancels refreshing a value
        /// </summary>
        /// <param name="key">The key of the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task CancelRefreshingAsync(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!(await TryCancelRefreshingAsync(key, cancellationToken).ConfigureAwait(false)))
                throw new KeyNotFoundException();
		}

        /// <summary>
        /// Cancels refreshing of all values
        /// </summary>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void CancelRefreshingAll()
        {
            ThrowIfDisposed();
			foreach (var key in buckets.Keys.ToList())
            {
                refreshAutoResetEvents.TryRemove(key, out _);
                if (refreshCancellationTokenSources.TryRemove(key, out var removedCancellationTokenSource))
                {
                    try
                    {
                        removedCancellationTokenSource.Cancel();
                        removedCancellationTokenSource.Dispose();
                    }
                    catch
                    {
                        // Allow this to happen silently
                    }
                }
            }
		}

        /// <summary>
        /// Cancels refreshing of all values
        /// </summary>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual Task CancelRefreshingAllAsync()
        {
            ThrowIfDisposed();
            var keys = buckets.Keys.ToList();
            return Task.Run(() =>
            {
                foreach (var key in keys)
                {
                    refreshAutoResetEvents.TryRemove(key, out _);
                    if (refreshCancellationTokenSources.TryRemove(key, out var removedCancellationTokenSource))
                    {
                        try
                        {
                            removedCancellationTokenSource.Cancel();
                            removedCancellationTokenSource.Dispose();
                        }
                        catch
                        {
                            // Allow this to happen silently
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Determines whether the cache contains a value
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was found in the cache; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual bool ContainsKey(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            DateTime? expired = null;
            var expiredValue = default(TValue);
            try
            {
                using (GetAsyncReaderWriterLock(key).ReaderLock(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!buckets.TryGetValue(key, out var bucket))
                        return false;
                    if (bucket.Expiration < DateTime.UtcNow)
                    {
                        buckets.TryRemove(key, out var removedBucket);
                        expired = removedBucket.Expiration;
                        removedBucket.TrySoftGetValue(out expiredValue);
                        return false;
                    }
                    return true;
                }
            }
            finally
            {
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
            }
        }

        /// <summary>
        /// Determines whether the cache contains a value
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was found in the cache; otherwise, false</returns>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual async Task<bool> ContainsKeyAsync(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            DateTime? expired = null;
            ThrowIfDisposed();
            var expiredValue = default(TValue);
            try
            {
                using (await GetAsyncReaderWriterLock(key).ReaderLockAsync(cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!buckets.TryGetValue(key, out var bucket))
                        throw new KeyNotFoundException();
                    if (bucket.Expiration < DateTime.UtcNow)
                    {
                        buckets.TryRemove(key, out var removedBucket);
                        expired = removedBucket.Expiration;
                        removedBucket.TrySoftGetValue(out expiredValue);
                        return false;
                    }
                    return true;
                }
            }
            finally
            {
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
            }
        }

		/// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                CancelRefreshingAll();
        }

		/// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
        /// <param name="cancellationToken">A token that can be used to attempt to cancel disposal</param>
        protected override async Task DisposeAsync(bool disposing, CancellationToken cancellationToken = default)
		{
			if (disposing)
				await CancelRefreshingAllAsync().ConfigureAwait(false);
		}

		/// <summary>
		/// Gets a value in the cache
		/// </summary>
		/// <param name="key">The key of the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
		/// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
		public TValue Get(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!TryGet(key, out var value, cancellationToken))
                throw new KeyNotFoundException();
            return value;
        }

        /// <summary>
        /// Gets a value in the cache
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public T Get<T>(TKey key, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            if (!TryGet(key, out T value, cancellationToken))
                throw new KeyNotFoundException();
            return value;
        }

        /// <summary>
        /// Gets a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="retryDelay">The amount of time to wait before attempting the get again</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task<TValue> GetAsync(TKey key, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default)
        {
            while (true)
			{
                ThrowIfDisposed();
                var result = await TryGetAsync(key, cancellationToken).ConfigureAwait(false);
                if (result.WasFound)
                    return result.Value;
                if (!(retryDelay is TimeSpan nonNullRetryDelay))
                    throw new KeyNotFoundException();
				await Task.Delay(nonNullRetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets a value in the cache
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="retryDelay">The amount of time to wait before attempting the get again</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task<T> GetAsync<T>(TKey key, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default) where T : TValue
        {
            while (true)
			{
                ThrowIfDisposed();
                var result = await TryGetAsync<T>(key, cancellationToken).ConfigureAwait(false);
                if (result.WasFound)
                    return result.Value;
                if (!(retryDelay is TimeSpan nonNullRetryDelay))
                    throw new KeyNotFoundException();
				await Task.Delay(nonNullRetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        AsyncReaderWriterLock GetAsyncReaderWriterLock(TKey key) => retrievalAccess.GetOrAdd(key, ReaderWriterLockFactory);

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TValue GetOrAdd(TKey key, TValue value, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAdd(key, ValueSource<TValue>.Create(value), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public T GetOrAdd<T>(TKey key, T value, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAdd(key, ValueSource<T>.Create(value), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TValue GetOrAdd(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAdd(key, ValueSource<TValue>.Create(valueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TValue GetOrAdd(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAdd(key, ValueSource<TValue>.Create(cancelableValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public T GetOrAdd<T>(TKey key, Func<T> valueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAdd(key, ValueSource<T>.Create(valueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public T GetOrAdd<T>(TKey key, Func<CancellationToken, T> cancelableValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAdd(key, ValueSource<T>.Create(cancelableValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TValue GetOrAddAndRefresh(TKey key, Func<TValue> valueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefresh(key, ValueSource<TValue>.Create(valueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TValue GetOrAddAndRefresh(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefresh(key, ValueSource<TValue>.Create(cancelableValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public T GetOrAddAndRefresh<T>(TKey key, Func<T> valueFactory, TimeSpan interval, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefresh(key, ValueSource<T>.Create(valueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public T GetOrAddAndRefresh<T>(TKey key, Func<CancellationToken, T> cancelableValueFactory, TimeSpan interval, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefresh(key, ValueSource<T>.Create(cancelableValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TValue> GetOrAddAndRefreshAsync(TKey key, Func<TValue> valueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefreshAsync(key, ValueSource<TValue>.Create(valueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TValue> GetOrAddAndRefreshAsync(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefreshAsync(key, ValueSource<TValue>.Create(cancelableValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<T> GetOrAddAndRefreshAsync<T>(TKey key, Func<T> valueFactory, TimeSpan interval, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefreshAsync(key, ValueSource<T>.Create(valueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<T> GetOrAddAndRefreshAsync<T>(TKey key, Func<CancellationToken, T> cancelableValueFactory, TimeSpan interval, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefreshAsync(key, ValueSource<T>.Create(cancelableValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TValue> GetOrAddAndRefreshAsync(TKey key, Func<Task<TValue>> asyncValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefreshAsync(key, ValueSource<TValue>.Create(asyncValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TValue> GetOrAddAndRefreshAsync(TKey key, Func<CancellationToken, Task<TValue>> cancelableAsyncValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefreshAsync(key, ValueSource<TValue>.Create(cancelableAsyncValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <returns>A task for which the result is the value</returns>
        public Task<T> GetOrAddAndRefreshAsync<T>(TKey key, Func<Task<T>> asyncValueFactory, TimeSpan interval, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefreshAsync(key, ValueSource<T>.Create(asyncValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<T> GetOrAddAndRefreshAsync<T>(TKey key, Func<CancellationToken, Task<T>> asyncValueFactory, TimeSpan interval, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAddAndRefreshAsync(key, ValueSource<T>.Create(asyncValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TValue> GetOrAddAsync(TKey key, TValue value, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAddAsync(key, ValueSource<TValue>.Create(value), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<T> GetOrAddAsync<T>(TKey key, T value, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAddAsync(key, ValueSource<T>.Create(value), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TValue> GetOrAddAsync(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAddAsync(key, ValueSource<TValue>.Create(valueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TValue> GetOrAddAsync(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAddAsync(key, ValueSource<TValue>.Create(cancelableValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<T> GetOrAddAsync<T>(TKey key, Func<T> valueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAddAsync(key, ValueSource<T>.Create(valueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<T> GetOrAddAsync<T>(TKey key, Func<CancellationToken, T> cancelableValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAddAsync(key, ValueSource<T>.Create(cancelableValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TValue> GetOrAddAsync(TKey key, Func<Task<TValue>> asyncValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAddAsync(key, ValueSource<TValue>.Create(asyncValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TValue> GetOrAddAsync(TKey key, Func<CancellationToken, Task<TValue>> cancelableAsyncValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformGetOrAddAsync(key, ValueSource<TValue>.Create(cancelableAsyncValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<T> GetOrAddAsync<T>(TKey key, Func<Task<T>> asyncValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAddAsync(key, ValueSource<T>.Create(asyncValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>The value</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<T> GetOrAddAsync<T>(TKey key, Func<CancellationToken, Task<T>> cancelableAsyncValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            return PerformGetOrAddAsync(key, ValueSource<T>.Create(cancelableAsyncValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
		/// Raises the <see cref="AfterReset"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnAfterReset(EventArgs e) => AfterReset?.Invoke(this, e);

		/// <summary>
		/// Raises the <see cref="BeforeReset"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnBeforeReset(EventArgs e) => BeforeReset?.Invoke(this, e);

		/// <summary>
		/// Raises the <see cref="ValueAdded"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnValueAdded(KeyValueEventArgs e) => ValueAdded?.Invoke(this, e);

		/// <summary>
		/// Raises the <see cref="ValueExpired"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnValueExpired(ValueExpiredEventArgs e) => ValueExpired?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="ValueAddFailed"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnValueAddFailed(KeyedExceptionEventArgs e) => ValueAddFailed?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="ValueReconstructed"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnValueReconstructed(KeyValueEventArgs e) => ValueReconstructed?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="ValueReferenceWeakened"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnValueReferenceWeakened(KeyValueEventArgs e) => ValueReferenceWeakened?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="ValueRefreshFailed"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnValueRefreshFailed(KeyedExceptionEventArgs e) => ValueRefreshFailed?.Invoke(this, e);

		/// <summary>
		/// Raises the <see cref="ValueRemoved"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnValueRemoved(KeyValueEventArgs e) => ValueRemoved?.Invoke(this, e);

		/// <summary>
		/// Raises the <see cref="ValueUpdated"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnValueUpdated(ValueUpdatedEventArgs e) => ValueUpdated?.Invoke(this, e);

        /// <summary>
		/// Raises the <see cref="ValueUpdateFailed"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnValueUpdateFailed(KeyedExceptionEventArgs e) => ValueUpdateFailed?.Invoke(this, e);

        /// <summary>
        /// Invoked when a caller adds a value to or updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueSource">The source of the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        protected virtual void PerformAddOrUpdate(TKey key, ValueSource<TValue> valueSource, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) =>
            WrapTimeout(ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                var updated = false;
                var oldValue = default(TValue);
                TValue value;
                using (GetAsyncReaderWriterLock(key).WriterLock(ct))
                {
                    ct.ThrowIfCancellationRequested();
                    value = valueSource.GetValue(ct);
                    if (buckets.TryGetValue(key, out var bucket))
                    {
                        if (bucket.Expiration < DateTime.UtcNow)
                        {
                            buckets.TryRemove(key, out var removedBucket);
                            expired = removedBucket.Expiration;
                            removedBucket.TrySoftGetValue(out expiredValue);
                        }
                        else
                        {
                            oldValue = bucket.GetValue(ct);
                            bucket.SetValue(value, true, ct);
                            bucket.Expiration = DateTime.UtcNow + expireIn;
                            updated = true;
                        }
                    }
                    if (!updated)
                        buckets.TryAdd(key, new Bucket(this, key, value, valueSource, expireIn));
                }
                if (updated)
                    OnValueUpdated(new ValueUpdatedEventArgs(key, oldValue, value, false));
                else
                {
                    if (expired != null)
                        OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                    OnValueAdded(new KeyValueEventArgs(key, value));
                }
            }, cancellationToken);

        /// <summary>
        /// Invoked when a caller adds a value to or updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueSource">The source of the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        protected virtual Task PerformAddOrUpdateAsync(TKey key, ValueSource<TValue> valueSource, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) =>
            WrapTimeoutAsync(async ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                var updated = false;
                var oldValue = default(TValue);
                TValue value;
                using (await GetAsyncReaderWriterLock(key).WriterLockAsync(ct).ConfigureAwait(false))
                {
                    ct.ThrowIfCancellationRequested();
                    if (valueSource.IsAsync)
                        value = await valueSource.GetValueAsync(ct).ConfigureAwait(false);
                    else
                    {
                        value = valueSource.GetValue(ct);
                        if (typeof(TValue) == typeof(object))
                            value = (TValue)(await TaskResolver.ResolveAsync(value).ConfigureAwait(false));
                    }
                    if (buckets.TryGetValue(key, out var bucket))
                    {
                        if (bucket.Expiration < DateTime.UtcNow)
                        {
                            buckets.TryRemove(key, out var removedBucket);
                            expired = removedBucket.Expiration;
                            removedBucket.TrySoftGetValue(out expiredValue);
                        }
                        else
                        {
                            oldValue = await bucket.GetValueAsync(ct).ConfigureAwait(false);
                            await bucket.SetValueAsync(value, true, ct).ConfigureAwait(false);
                            bucket.Expiration = DateTime.UtcNow + expireIn;
                            updated = true;
                        }
                    }
                    if (!updated)
                        buckets.TryAdd(key, new Bucket(this, key, value, valueSource, expireIn));
                }
                if (updated)
                    OnValueUpdated(new ValueUpdatedEventArgs(key, oldValue, value, false));
                else
                {
                    if (expired != null)
                        OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                    OnValueAdded(new KeyValueEventArgs(key, value));
                }
            }, cancellationToken);

        /// <summary>
        /// Invoked when a caller gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="valueSource">The source of the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <returns>The value</returns>
        /// <exception cref="InvalidCastException">The value associated to <paramref name="key"/> cannot be cast to <typeparamref name="T"/></exception>
        protected virtual T PerformGetOrAdd<T>(TKey key, ValueSource<T> valueSource, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) where T : TValue =>
            WrapTimeout(ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                var added = false;
                var value = default(T);
                try
                {
                    using (GetAsyncReaderWriterLock(key).ReaderLock(ct))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (buckets.TryGetValue(key, out var bucket))
                        {
                            if (bucket.Expiration < DateTime.UtcNow)
                            {
                                buckets.TryRemove(key, out var removedBucket);
                                expired = removedBucket.Expiration;
                                removedBucket.TrySoftGetValue(out expiredValue);
                            }
                            else if (bucket.GetValue(ct) is T typedValue)
                                return typedValue;
                            else
                                throw new InvalidCastException();
                        }
                    }
                    using (GetAsyncReaderWriterLock(key).WriterLock(ct))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (buckets.TryGetValue(key, out var bucket))
                        {
                            if (bucket.Expiration < DateTime.UtcNow)
                            {
                                buckets.TryRemove(key, out var removedBucket);
                                expired = removedBucket.Expiration;
                                removedBucket.TrySoftGetValue(out expiredValue);
                            }
                            else if (bucket.GetValue(ct) is T typedValue)
                                return typedValue;
                            else
                                throw new InvalidCastException();
                        }
                        value = valueSource.GetValue(ct);
                        buckets.TryAdd(key, new Bucket(this, key, value, valueSource.CreateContravariantValueSource<TValue>(), expireIn));
                        added = true;
                        return value;
                    }
                }
                finally
                {
                    if (expired != null)
                        OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                    if (added)
                        OnValueAdded(new KeyValueEventArgs(key, value));
                }
            }, cancellationToken);

        /// <summary>
        /// Invoked when a caller gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <returns>The value</returns>
        /// <exception cref="InvalidCastException">The value associated to <paramref name="key"/> cannot be cast to <typeparamref name="T"/></exception>
        protected virtual T PerformGetOrAddAndRefresh<T>(TKey key, ValueSource<T> valueFactory, TimeSpan interval, CancellationToken cancellationToken = default) where T : TValue =>
            WrapTimeout(ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                Bucket addedBucket = null;
                T value = default;
                try
                {
                    using (GetAsyncReaderWriterLock(key).ReaderLock(ct))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (buckets.TryGetValue(key, out var bucket))
                        {
                            if (bucket.Expiration < DateTime.UtcNow)
                            {
                                buckets.TryRemove(key, out var removedBucket);
                                expired = removedBucket.Expiration;
                                removedBucket.TrySoftGetValue(out expiredValue);
                            }
                            else if (bucket.GetValue(ct) is T typedValue)
                                return typedValue;
                            else
                                throw new InvalidCastException();
                        }
                    }
                    using (GetAsyncReaderWriterLock(key).WriterLock(ct))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (buckets.TryGetValue(key, out var bucket))
                        {
                            if (bucket.Expiration < DateTime.UtcNow)
                            {
                                buckets.TryRemove(key, out var removedBucket);
                                expired = removedBucket.Expiration;
                                removedBucket.TrySoftGetValue(out expiredValue);
                            }
                            else if (bucket.GetValue(ct) is T typedValue)
                                return typedValue;
                            else
                                throw new InvalidCastException();
                        }
                        value = valueFactory.GetValue(ct);
                        addedBucket = new Bucket(this, key, value, valueFactory.CreateContravariantValueSource<TValue>());
                        buckets.TryAdd(key, addedBucket);
                        return value;
                    }
                }
                finally
                {
                    if (expired != null)
                        OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                    if (addedBucket != null)
                    {
                        var addedBucketId = addedBucket.Id;
                        var refreshCancellationToken = refreshCancellationTokenSources.AddOrUpdate(key, CancellationTokenSourceFactory, CancellationTokenSourceFactory).Token;
                        var refreshAutoResetEvent = refreshAutoResetEvents.AddOrUpdate(key, AutoResetEventFactory, AutoResetEventFactory);
                        Task.Run(async () =>
                        {
                            while (true)
                            {
                                try
                                {
                                    using (var manualResetEventCancellationTokenSource = new CancellationTokenSource())
                                    {
                                        await Task.WhenAny(Task.Delay(interval, refreshCancellationToken), refreshAutoResetEvent.WaitAsync(manualResetEventCancellationTokenSource.Token)).ConfigureAwait(false);
                                        await Task.Run(() =>
                                        {
                                            try
                                            {
                                                manualResetEventCancellationTokenSource.Cancel();
                                            }
                                            catch (ObjectDisposedException)
                                            {
                                                // Allow this to happen silently
                                            }
                                        }).ConfigureAwait(false);
                                    }
                                    refreshCancellationToken.ThrowIfCancellationRequested();
                                    TValue oldValue;
                                    using (await GetAsyncReaderWriterLock(key).ReaderLockAsync(refreshCancellationToken).ConfigureAwait(false))
                                    {
                                        if (!buckets.TryGetValue(key, out var refreshingBucket) || refreshingBucket.Id != addedBucketId)
                                            break;
                                        if (!refreshingBucket.TrySoftGetValue(out oldValue))
                                            continue;
                                    }
                                    var newValue = valueFactory.GetValue(refreshCancellationToken);
                                    using (await GetAsyncReaderWriterLock(key).WriterLockAsync(refreshCancellationToken).ConfigureAwait(false))
                                    {
                                        if (!buckets.TryGetValue(key, out var refreshingBucket) || refreshingBucket.Id != addedBucketId)
                                            break;
                                        await refreshingBucket.SetValueAsync(newValue, false, refreshCancellationToken).ConfigureAwait(false);
                                    }
                                    OnValueUpdated(new ValueUpdatedEventArgs(key, oldValue, newValue, true));
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    try
                                    {
                                        OnValueRefreshFailed(new KeyedExceptionEventArgs(key, ex));
                                    }
                                    catch
                                    {
                                        // An unhandled exception thrown by an event handler must not be allowed to prevent subsequent refreshing
                                    }
                                }
                            }
                        });
                        OnValueAdded(new KeyValueEventArgs(key, value));
                    }
                }
            }, cancellationToken);

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present and refreshes it
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="valueSource">The source of the value to add if the value is not present in the cache</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <returns>The value</returns>
        /// <exception cref="InvalidCastException">The value associated to <paramref name="key"/> cannot be cast to <typeparamref name="T"/></exception>
        protected virtual Task<T> PerformGetOrAddAndRefreshAsync<T>(TKey key, ValueSource<T> valueSource, TimeSpan interval, CancellationToken cancellationToken = default) where T : TValue =>
            WrapTimeoutAsync(async ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                Bucket addedBucket = null;
                T value = default;
                try
                {
                    using (await GetAsyncReaderWriterLock(key).ReaderLockAsync(ct).ConfigureAwait(false))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (buckets.TryGetValue(key, out var bucket))
                        {
                            if (bucket.Expiration < DateTime.UtcNow)
                            {
                                buckets.TryRemove(key, out var removedBucket);
                                expired = removedBucket.Expiration;
                                removedBucket.TrySoftGetValue(out expiredValue);
                            }
                            else if (await bucket.GetValueAsync(ct).ConfigureAwait(false) is T typedValue)
                                return typedValue;
                            else
                                throw new InvalidCastException();
                        }
                    }
                    using (await GetAsyncReaderWriterLock(key).WriterLockAsync(ct).ConfigureAwait(false))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (buckets.TryGetValue(key, out var bucket))
                        {
                            if (bucket.Expiration < DateTime.UtcNow)
                            {
                                buckets.TryRemove(key, out var removedBucket);
                                expired = removedBucket.Expiration;
                                removedBucket.TrySoftGetValue(out expiredValue);
                            }
                            else if (await bucket.GetValueAsync(ct).ConfigureAwait(false) is T typedValue)
                                return typedValue;
                            else
                                throw new InvalidCastException();
                        }
                        if (valueSource.IsAsync)
                            value = await valueSource.GetValueAsync(ct).ConfigureAwait(false);
                        else
                        {
                            value = valueSource.GetValue(ct);
                            if (typeof(TValue) == typeof(object))
                                value = (T)(await TaskResolver.ResolveAsync(value).ConfigureAwait(false));
                        }
                        addedBucket = new Bucket(this, key, value, valueSource.CreateContravariantValueSource<TValue>());
                        buckets.TryAdd(key, addedBucket);
                        return value;
                    }
                }
                finally
                {
                    if (expired != null)
                        OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                    if (addedBucket != null)
                    {
                        var addedBucketId = addedBucket.Id;
                        var refreshCancellationToken = refreshCancellationTokenSources.AddOrUpdate(key, CancellationTokenSourceFactory, CancellationTokenSourceFactory).Token;
                        var refreshAutoResetEvent = refreshAutoResetEvents.AddOrUpdate(key, AutoResetEventFactory, AutoResetEventFactory);
#pragma warning disable CS4014
                        Task.Run(async () =>
                        {
                            while (true)
                            {
                                try
                                {
                                    using (var manualResetEventCancellationTokenSource = new CancellationTokenSource())
                                    {
                                        await Task.WhenAny(Task.Delay(interval, refreshCancellationToken), refreshAutoResetEvent.WaitAsync(manualResetEventCancellationTokenSource.Token)).ConfigureAwait(false);
                                        await Task.Run(() =>
                                        {
                                            try
                                            {
                                                manualResetEventCancellationTokenSource.Cancel();
                                            }
                                            catch (ObjectDisposedException)
                                            {
                                                // Allow this to happen silently
                                            }
                                        }).ConfigureAwait(false);
                                    }
                                    refreshCancellationToken.ThrowIfCancellationRequested();
                                    TValue oldValue;
                                    using (await GetAsyncReaderWriterLock(key).ReaderLockAsync(refreshCancellationToken).ConfigureAwait(false))
                                    {
                                        if (!buckets.TryGetValue(key, out var refreshingBucket) || refreshingBucket.Id != addedBucketId)
                                            break;
                                        if (!refreshingBucket.TrySoftGetValue(out oldValue))
                                            continue;
                                    }
                                    T newValue;
                                    if (valueSource.IsAsync)
                                        newValue = await valueSource.GetValueAsync(refreshCancellationToken).ConfigureAwait(false);
                                    else
                                    {
                                        newValue = valueSource.GetValue(refreshCancellationToken);
                                        if (typeof(TValue) == typeof(object))
                                            newValue = (T)(await TaskResolver.ResolveAsync(newValue).ConfigureAwait(false));
                                    }
                                    using (await GetAsyncReaderWriterLock(key).WriterLockAsync(refreshCancellationToken).ConfigureAwait(false))
                                    {
                                        if (!buckets.TryGetValue(key, out var refreshingBucket) || refreshingBucket.Id != addedBucketId)
                                            break;
                                        await refreshingBucket.SetValueAsync(newValue, false, refreshCancellationToken).ConfigureAwait(false);
                                    }
                                    OnValueUpdated(new ValueUpdatedEventArgs(key, oldValue, newValue, true));
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    try
                                    {
                                        OnValueRefreshFailed(new KeyedExceptionEventArgs(key, ex));
                                    }
                                    catch
                                    {
                                        // An unhandled exception thrown by an event handler must not be allowed to prevent subsequent refreshing
                                    }
                                }
                            }
                        });
#pragma warning restore CS4014
                        OnValueAdded(new KeyValueEventArgs(key, value));
                    }
                }
            }, cancellationToken);

        /// <summary>
        /// Gets a value in the cache or adds it to the cache if it is not already present
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="valueSource">The source of the value to add if the value is not present in the cache</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <returns>The value</returns>
        /// <exception cref="InvalidCastException">The value associated to <paramref name="key"/> cannot be cast to <typeparamref name="T"/></exception>
        protected virtual Task<T> PerformGetOrAddAsync<T>(TKey key, ValueSource<T> valueSource, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) where T : TValue =>
            WrapTimeoutAsync(async ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                var added = false;
                var value = default(T);
                try
                {
                    using (await GetAsyncReaderWriterLock(key).ReaderLockAsync(ct).ConfigureAwait(false))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (buckets.TryGetValue(key, out var bucket))
                        {
                            if (bucket.Expiration < DateTime.UtcNow)
                            {
                                buckets.TryRemove(key, out var removedBucket);
                                expired = removedBucket.Expiration;
                                removedBucket.TrySoftGetValue(out expiredValue);
                            }
                            else if (await bucket.GetValueAsync(ct).ConfigureAwait(false) is T typedValue)
                                return typedValue;
                            else
                                throw new InvalidCastException();
                        }
                    }
                    using (await GetAsyncReaderWriterLock(key).WriterLockAsync(ct).ConfigureAwait(false))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (buckets.TryGetValue(key, out var bucket))
                        {
                            if (bucket.Expiration < DateTime.UtcNow)
                            {
                                buckets.TryRemove(key, out var removedBucket);
                                expired = removedBucket.Expiration;
                                removedBucket.TrySoftGetValue(out expiredValue);
                            }
                            else if (await bucket.GetValueAsync(ct).ConfigureAwait(false) is T typedValue)
                                return typedValue;
                            else
                                throw new InvalidCastException();
                        }
                        if (valueSource.IsAsync)
                            value = await valueSource.GetValueAsync(ct).ConfigureAwait(false);
                        else
                        {
                            value = valueSource.GetValue(ct);
                            if (typeof(TValue) == typeof(object))
                                value = (T)(await TaskResolver.ResolveAsync(value).ConfigureAwait(false));
                        }
                        buckets.TryAdd(key, new Bucket(this, key, value, valueSource.CreateContravariantValueSource<TValue>(), expireIn));
                        added = true;
                        return value;
                    }
                }
                finally
                {
                    if (expired != null)
                        OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                    if (added)
                        OnValueAdded(new KeyValueEventArgs(key, value));
                }
            }, cancellationToken);

        /// <summary>
        /// Invoked when a caller tries to add a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueSource">The value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <returns>true is the value was added; otherwise, false</returns>
        protected virtual TryOperationResult PerformTryAdd(TKey key, ValueSource<TValue> valueSource, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) =>
            WrapTimeout(ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                TValue value;
                using (GetAsyncReaderWriterLock(key).WriterLock(ct))
                {
                    ct.ThrowIfCancellationRequested();
                    if (buckets.TryGetValue(key, out var bucket))
                    {
                        if (bucket.Expiration < DateTime.UtcNow)
                        {
                            buckets.TryRemove(key, out var removedBucket);
                            expired = removedBucket.Expiration;
                            removedBucket.TrySoftGetValue(out expiredValue);
                        }
                        else
                            return TryOperationResult.DuplicateKey;
                    }
                    try
                    {
                        value = valueSource.GetValue(ct);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            OnValueAddFailed(new KeyedExceptionEventArgs(key, ex));
                        }
                        catch
                        {
                            // Nothing we can do about this
                        }
                        return new TryOperationResult(ex);
                    }
                    buckets.TryAdd(key, new Bucket(this, key, value, valueSource, expireIn));
                }
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                OnValueAdded(new KeyValueEventArgs(key, value));
                return TryOperationResult.Succeeded;
            }, cancellationToken);

        /// <summary>
        /// Tries to add a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueSource">The source of the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <returns>true if the value was added; otherwise, false</returns>
        protected virtual Task<TryOperationResult> PerformTryAddAsync(TKey key, ValueSource<TValue> valueSource, TimeSpan? expireIn = null, CancellationToken cancellationToken = default) =>
            WrapTimeoutAsync(async ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                TValue value;
                using (await GetAsyncReaderWriterLock(key).WriterLockAsync(ct).ConfigureAwait(false))
                {
                    ct.ThrowIfCancellationRequested();
                    if (buckets.TryGetValue(key, out var bucket))
                    {
                        if (bucket.Expiration < DateTime.UtcNow)
                        {
                            buckets.TryRemove(key, out var removedBucket);
                            expired = removedBucket.Expiration;
                            removedBucket.TrySoftGetValue(out expiredValue);
                        }
                        else
                            return TryOperationResult.DuplicateKey;
                    }
                    try
                    {
                        if (valueSource.IsAsync)
                            value = await valueSource.GetValueAsync(ct).ConfigureAwait(false);
                        else
                        {
                            value = valueSource.GetValue(ct);
                            if (typeof(TValue) == typeof(object))
                                value = (TValue)(await TaskResolver.ResolveAsync(value).ConfigureAwait(false));
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            OnValueAddFailed(new KeyedExceptionEventArgs(key, ex));
                        }
                        catch
                        {
                            // Nothing we can do about this
                        }
                        return new TryOperationResult(ex);
                    }
                    buckets.TryAdd(key, new Bucket(this, key, value, valueSource, expireIn));
                }
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                OnValueAdded(new KeyValueEventArgs(key, value));
                return TryOperationResult.Succeeded;
            }, cancellationToken);

        /// <summary>
        /// Tries to add a value to the cache and refresh it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueSource">The source of the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <returns>true if the value was added; otherwise, false</returns>
        protected virtual TryOperationResult PerformTryAddAndRefresh(TKey key, ValueSource<TValue> valueSource, TimeSpan interval, CancellationToken cancellationToken = default) =>
            WrapTimeout(ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                TValue value;
                using (GetAsyncReaderWriterLock(key).WriterLock(ct))
                {
                    ct.ThrowIfCancellationRequested();
                    if (buckets.TryGetValue(key, out var bucket))
                    {
                        if (bucket.Expiration < DateTime.UtcNow)
                        {
                            buckets.TryRemove(key, out var removedBucket);
                            expired = removedBucket.Expiration;
                            removedBucket.TrySoftGetValue(out expiredValue);
                        }
                        else
                            return TryOperationResult.DuplicateKey;
                    }
                    try
                    {
                        value = valueSource.GetValue(ct);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            OnValueAddFailed(new KeyedExceptionEventArgs(key, ex));
                        }
                        catch
                        {
                            // Nothing we can do about this
                        }
                        return new TryOperationResult(ex);
                    }
                    var addedBucket = new Bucket(this, key, value, valueSource);
                    var addedBucketId = addedBucket.Id;
                    buckets.TryAdd(key, addedBucket);
                    var refreshCancellationToken = refreshCancellationTokenSources.AddOrUpdate(key, CancellationTokenSourceFactory, CancellationTokenSourceFactory).Token;
                    var refreshAutoResetEvent = refreshAutoResetEvents.AddOrUpdate(key, AutoResetEventFactory, AutoResetEventFactory);
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            try
                            {
                                using (var manualResetEventCancellationTokenSource = new CancellationTokenSource())
                                {
                                    await Task.WhenAny(Task.Delay(interval, refreshCancellationToken), refreshAutoResetEvent.WaitAsync(manualResetEventCancellationTokenSource.Token)).ConfigureAwait(false);
                                    await Task.Run(() =>
                                    {
                                        try
                                        {
                                            manualResetEventCancellationTokenSource.Cancel();
                                        }
                                        catch (ObjectDisposedException)
                                        {
                                            // Allow this to happen silently
                                        }
                                    }).ConfigureAwait(false);
                                }
                                refreshCancellationToken.ThrowIfCancellationRequested();
                                TValue oldValue;
                                using (await GetAsyncReaderWriterLock(key).ReaderLockAsync(refreshCancellationToken).ConfigureAwait(false))
                                {
                                    if (!buckets.TryGetValue(key, out var refreshingBucket) || refreshingBucket.Id != addedBucketId)
                                        break;
                                    if (!refreshingBucket.TrySoftGetValue(out oldValue))
                                        continue;
                                }
                                var newValue = valueSource.GetValue(refreshCancellationToken);
                                using (await GetAsyncReaderWriterLock(key).WriterLockAsync(refreshCancellationToken).ConfigureAwait(false))
                                {
                                    if (!buckets.TryGetValue(key, out var refreshingBucket) || refreshingBucket.Id != addedBucketId)
                                        break;
                                    await refreshingBucket.SetValueAsync(newValue, false, refreshCancellationToken).ConfigureAwait(false);
                                }
                                OnValueUpdated(new ValueUpdatedEventArgs(key, oldValue, newValue, true));
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    OnValueRefreshFailed(new KeyedExceptionEventArgs(key, ex));
                                }
                                catch
                                {
                                    // An unhandled exception thrown by an event handler must not be allowed to prevent subsequent refreshing
                                }
                            }
                        }
                    });
                }
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                OnValueAdded(new KeyValueEventArgs(key, value));
                return TryOperationResult.Succeeded;
            }, cancellationToken);

        /// <summary>
        /// Tries to add a value to the cache and refresh it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueSource">The source of the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <returns>true if the value was added; otherwise, false</returns>
        protected virtual Task<TryOperationResult> PerformTryAddAndRefreshAsync(TKey key, ValueSource<TValue> valueSource, TimeSpan interval, CancellationToken cancellationToken = default) =>
            WrapTimeout(async ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                TValue value;
                using (await GetAsyncReaderWriterLock(key).WriterLockAsync(ct).ConfigureAwait(false))
                {
                    ct.ThrowIfCancellationRequested();
                    if (buckets.TryGetValue(key, out var bucket))
                    {
                        if (bucket.Expiration < DateTime.UtcNow)
                        {
                            buckets.TryRemove(key, out var removedBucket);
                            expired = removedBucket.Expiration;
                            removedBucket.TrySoftGetValue(out expiredValue);
                        }
                        else
                            return TryOperationResult.DuplicateKey;
                    }
                    try
                    {
                        if (valueSource.IsAsync)
                            value = await valueSource.GetValueAsync(ct).ConfigureAwait(false);
                        else
                        {
                            value = valueSource.GetValue(ct);
                            if (typeof(TValue) == typeof(object))
                                value = (TValue)(await TaskResolver.ResolveAsync(value).ConfigureAwait(false));
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            OnValueAddFailed(new KeyedExceptionEventArgs(key, ex));
                        }
                        catch
                        {
                            // Nothing we can do about this
                        }
                        return new TryOperationResult(ex);
                    }
                    var addedBucket = new Bucket(this, key, value, valueSource);
                    var addedBucketId = addedBucket.Id;
                    buckets.TryAdd(key, addedBucket);
                    var refreshCancellationToken = refreshCancellationTokenSources.AddOrUpdate(key, CancellationTokenSourceFactory, CancellationTokenSourceFactory).Token;
                    var refreshAutoResetEvent = refreshAutoResetEvents.AddOrUpdate(key, AutoResetEventFactory, AutoResetEventFactory);
#pragma warning disable CS4014
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            try
                            {
                                using (var manualResetEventCancellationTokenSource = new CancellationTokenSource())
                                {
                                    await Task.WhenAny(Task.Delay(interval, refreshCancellationToken), refreshAutoResetEvent.WaitAsync(manualResetEventCancellationTokenSource.Token)).ConfigureAwait(false);
                                    await Task.Run(() =>
                                    {
                                        try
                                        {
                                            manualResetEventCancellationTokenSource.Cancel();
                                        }
                                        catch (ObjectDisposedException)
                                        {
                                            // Allow this to happen silently
                                        }
                                    }).ConfigureAwait(false);
                                }
                                refreshCancellationToken.ThrowIfCancellationRequested();
                                TValue oldValue;
                                using (await GetAsyncReaderWriterLock(key).ReaderLockAsync(refreshCancellationToken).ConfigureAwait(false))
                                {
                                    if (!buckets.TryGetValue(key, out var refreshingBucket) || refreshingBucket.Id != addedBucketId)
                                        break;
                                    if (!refreshingBucket.TrySoftGetValue(out oldValue))
                                        continue;
                                }
                                TValue newValue;
                                if (valueSource.IsAsync)
                                    newValue = await valueSource.GetValueAsync(refreshCancellationToken).ConfigureAwait(false);
                                else
                                {
                                    newValue = valueSource.GetValue(refreshCancellationToken);
                                    if (typeof(TValue) == typeof(object))
                                        newValue = (TValue)(await TaskResolver.ResolveAsync(newValue).ConfigureAwait(false));
                                }
                                using (await GetAsyncReaderWriterLock(key).WriterLockAsync(refreshCancellationToken).ConfigureAwait(false))
                                {
                                    if (!buckets.TryGetValue(key, out var refreshingBucket) || refreshingBucket.Id != addedBucketId)
                                        break;
                                    await refreshingBucket.SetValueAsync(newValue, false, refreshCancellationToken).ConfigureAwait(false);
                                }
                                OnValueUpdated(new ValueUpdatedEventArgs(key, oldValue, newValue, true));
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    OnValueRefreshFailed(new KeyedExceptionEventArgs(key, ex));
                                }
                                catch
                                {
                                    // An unhandled exception thrown by an event handler must not be allowed to prevent subsequent refreshing
                                }
                            }
                        }
                    });
#pragma warning restore CS4014
                }
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                OnValueAdded(new KeyValueEventArgs(key, value));
                return TryOperationResult.Succeeded;
            }, cancellationToken);

        /// <summary>
        /// Tries to update a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueSource">The source of the value</param>
        /// <param name="setExpiration">When true, expiration is updated with <paramref name="expireIn"/></param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <returns>true if the value was updated; otherwise, false</returns>
        protected virtual TryOperationResult PerformTryUpdate(TKey key, ValueSource<TValue> valueSource, bool setExpiration, TimeSpan? expireIn, CancellationToken cancellationToken = default) =>
            WrapTimeout(ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                try
                {
                    TValue value, oldValue;
                    using (GetAsyncReaderWriterLock(key).WriterLock(ct))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (!buckets.TryGetValue(key, out var bucket))
                            return TryOperationResult.KeyNotFound;
                        if (bucket.Expiration < DateTime.UtcNow)
                        {
                            buckets.TryRemove(key, out var removedBucket);
                            expired = bucket.Expiration.Value;
                            bucket.TrySoftGetValue(out expiredValue);
                            return TryOperationResult.KeyNotFound;
                        }
                        try
                        {
                            value = valueSource.GetValue(ct);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                OnValueUpdateFailed(new KeyedExceptionEventArgs(key, ex));
                            }
                            catch
                            {
                                // Nothing we can do about this
                            }
                            return new TryOperationResult(ex);
                        }
                        oldValue = bucket.GetValue(ct);
                        bucket.SetValue(value, true, ct);
                        if (setExpiration)
                            bucket.Expiration = DateTime.UtcNow + expireIn;
                    }
                    OnValueUpdated(new ValueUpdatedEventArgs(key, oldValue, value, false));
                    return TryOperationResult.Succeeded;
                }
                finally
                {
                    if (expired != null)
                        OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                }
            }, cancellationToken);

        /// <summary>
        /// Tries to update a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueSource">A factory to produce the value</param>
        /// <param name="setExpiration">When true, expiration is updated with <paramref name="expireIn"/></param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
        /// <returns>true if the value was updated; otherwise, false</returns>
        protected virtual Task<TryOperationResult> PerformTryUpdateAsync(TKey key, ValueSource<TValue> valueSource, bool setExpiration, TimeSpan? expireIn, CancellationToken cancellationToken = default) =>
            WrapTimeoutAsync(async ct =>
            {
                DateTime? expired = null;
                var expiredValue = default(TValue);
                try
                {
                    TValue value, oldValue;
                    using (await GetAsyncReaderWriterLock(key).WriterLockAsync(ct).ConfigureAwait(false))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (!buckets.TryGetValue(key, out var bucket))
                            return TryOperationResult.KeyNotFound;
                        if (bucket.Expiration < DateTime.UtcNow)
                        {
                            buckets.TryRemove(key, out var removedBucket);
                            expired = bucket.Expiration.Value;
                            bucket.TrySoftGetValue(out expiredValue);
                            return TryOperationResult.KeyNotFound;
                        }
                        try
                        {
                            if (valueSource.IsAsync)
                                value = await valueSource.GetValueAsync(ct).ConfigureAwait(false);
                            else
                            {
                                value = valueSource.GetValue(ct);
                                if (typeof(TValue) == typeof(object))
                                    value = (TValue)(await TaskResolver.ResolveAsync(value).ConfigureAwait(false));
                            }
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                OnValueUpdateFailed(new KeyedExceptionEventArgs(key, ex));
                            }
                            catch
                            {
                                // Nothing we can do about this
                            }
                            return new TryOperationResult(ex);
                        }
                        oldValue = await bucket.GetValueAsync(ct).ConfigureAwait(false);
                        await bucket.SetValueAsync(value, true, ct).ConfigureAwait(false);
                        if (setExpiration)
                            bucket.Expiration = DateTime.UtcNow + expireIn;
                    }
                    OnValueUpdated(new ValueUpdatedEventArgs(key, oldValue, value, false));
                    return TryOperationResult.Succeeded;
                }
                finally
                {
                    if (expired != null)
                        OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
                }
            }, cancellationToken);

        static AsyncReaderWriterLock ReaderWriterLockFactory(TKey key) => new AsyncReaderWriterLock();

        /// <summary>
        /// Removes a value from the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void Remove(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!TryRemove(key, cancellationToken))
                throw new KeyNotFoundException();
        }

        /// <summary>
        /// Removes a value from the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!(await TryRemoveAsync(key, cancellationToken).ConfigureAwait(false)))
                throw new KeyNotFoundException();
		}

        /// <summary>
        /// Cancels any refreshing of values in the cache and removes all values from the cache
        /// </summary>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual void Reset()
        {
            ThrowIfDisposed();
            OnBeforeReset(new EventArgs());
			CancelRefreshingAllAsync().Wait();
            if (comparer == null)
            {
                buckets = new ConcurrentDictionary<TKey, Bucket>();
                retrievalAccess = new ConcurrentDictionary<TKey, AsyncReaderWriterLock>();
            }
            else
            {
                buckets = new ConcurrentDictionary<TKey, Bucket>(comparer);
                retrievalAccess = new ConcurrentDictionary<TKey, AsyncReaderWriterLock>(comparer);
            }
            OnAfterReset(new EventArgs());
		}

        /// <summary>
        /// Cancels any refreshing of values in the cache and removes all values from the cache
        /// </summary>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual async Task ResetAsync()
        {
            ThrowIfDisposed();
            OnBeforeReset(new EventArgs());
            await CancelRefreshingAllAsync().ConfigureAwait(false);
            if (comparer == null)
            {
                buckets = new ConcurrentDictionary<TKey, Bucket>();
                retrievalAccess = new ConcurrentDictionary<TKey, AsyncReaderWriterLock>();
            }
            else
            {
                buckets = new ConcurrentDictionary<TKey, Bucket>(comparer);
                retrievalAccess = new ConcurrentDictionary<TKey, AsyncReaderWriterLock>(comparer);
            }
            OnAfterReset(new EventArgs());
        }

        /// <summary>
        /// Signals refreshing a value
        /// </summary>
        /// <param name="key">The key of the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void SignalRefresh(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!TrySignalRefresh(key, cancellationToken))
                throw new KeyNotFoundException();
        }

        /// <summary>
        /// Signals refreshing a value
        /// </summary>
        /// <param name="key">The key of the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task SignalRefreshAsync(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!(await TrySignalRefreshAsync(key, cancellationToken).ConfigureAwait(false)))
                throw new KeyNotFoundException();
        }

        /// <summary>
        /// Signals refreshing all values
		/// </summary>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual void SignalRefreshAll()
        {
            ThrowIfDisposed();
            foreach (var key in buckets.Keys.ToList())
                if (refreshAutoResetEvents.TryGetValue(key, out var autoResetEvent))
                    autoResetEvent.Set();
        }

        /// <summary>
        /// Tries to add a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true is the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TryOperationResult TryAdd(TKey key, TValue value, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAdd(key, ValueSource<TValue>.Create(value), expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true is the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TryOperationResult TryAdd(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAdd(key, ValueSource<TValue>.Create(cancelableValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true is the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TryOperationResult TryAdd(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAdd(key, ValueSource<TValue>.Create(valueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryAddAsync(TKey key, TValue value, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAddAsync(key, ValueSource<TValue>.Create(value), expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryAddAsync(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAddAsync(key, ValueSource<TValue>.Create(valueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryAddAsync(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAddAsync(key, ValueSource<TValue>.Create(cancelableValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryAddAsync(TKey key, Func<CancellationToken, Task<TValue>> cancelableAsyncValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAddAsync(key, ValueSource<TValue>.Create(cancelableAsyncValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryAddAsync(TKey key, Func<Task<TValue>> asyncValueFactory, TimeSpan? expireIn = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAddAsync(key, ValueSource<TValue>.Create(asyncValueFactory), expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache and refresh it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TryOperationResult TryAddAndRefresh(TKey key, Func<TValue> valueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAddAndRefresh(key, ValueSource<TValue>.Create(valueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache and refresh it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TryOperationResult TryAddAndRefresh(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAddAndRefresh(key, ValueSource<TValue>.Create(cancelableValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache and refresh it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryAddAndRefreshAsync(TKey key, Func<TValue> valueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAddAndRefreshAsync(key, ValueSource<TValue>.Create(valueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache and refresh it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryAddAndRefreshAsync(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAddAndRefreshAsync(key, ValueSource<TValue>.Create(cancelableValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache and refresh it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryAddAndRefreshAsync(TKey key, Func<Task<TValue>> asyncValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAddAndRefreshAsync(key, ValueSource<TValue>.Create(asyncValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Tries to add a value to the cache and refresh it
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value</param>
        /// <param name="interval">The amount of time between refreshes</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was added; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryAddAndRefreshAsync(TKey key, Func<CancellationToken, Task<TValue>> cancelableAsyncValueFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryAddAndRefreshAsync(key, ValueSource<TValue>.Create(cancelableAsyncValueFactory), interval, cancellationToken);
        }

        /// <summary>
        /// Tries to cancel refreshing a value
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if refreshing of the value was cancelled; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual bool TryCancelRefreshing(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            DateTime? expired = null;
            var expiredValue = default(TValue);
            try
            {
                using (GetAsyncReaderWriterLock(key).WriterLock(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!buckets.TryGetValue(key, out var bucket))
                        return false;
                    if (bucket.Expiration < DateTime.UtcNow)
                    {
                        buckets.TryRemove(key, out var removedBucket);
                        expired = removedBucket.Expiration;
                        removedBucket.TrySoftGetValue(out expiredValue);
                        return false;
                    }
                    var wasAutoResetEventRemoved = refreshAutoResetEvents.TryRemove(key, out var removedAutoResetEvent);
                    var wasCancellationTokenSourceRemoved = refreshCancellationTokenSources.TryRemove(key, out var removedCancellationTokenSource);
                    if (wasCancellationTokenSourceRemoved)
                        Task.Run(() =>
                        {
                            try
                            {
                                removedCancellationTokenSource.Cancel();
                                removedCancellationTokenSource.Dispose();
                            }
                            catch
                            {
                                // Allow this to happen silently
                            }
                        });
                    return wasAutoResetEventRemoved && wasCancellationTokenSourceRemoved;
                }
            }
            finally
            {
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
            }
        }

        /// <summary>
        /// Tries to cancel refreshing a value
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if refreshing of the value was cancelled; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual async Task<bool> TryCancelRefreshingAsync(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            DateTime? expired = null;
            var expiredValue = default(TValue);
            try
            {
                using (await GetAsyncReaderWriterLock(key).WriterLockAsync(cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!buckets.TryGetValue(key, out var bucket))
                        return false;
                    if (bucket.Expiration < DateTime.UtcNow)
                    {
                        buckets.TryRemove(key, out var removedBucket);
                        expired = removedBucket.Expiration;
                        removedBucket.TrySoftGetValue(out expiredValue);
                        return false;
                    }
                    var wasAutoResetEventRemoved = refreshAutoResetEvents.TryRemove(key, out var removedAutoResetEvent);
                    var wasCancellationTokenSourceRemoved = refreshCancellationTokenSources.TryRemove(key, out var removedCancellationTokenSource);
                    if (wasCancellationTokenSourceRemoved)
#pragma warning disable CS4014
                        Task.Run(() =>
                        {
                            try
                            {
                                removedCancellationTokenSource.Cancel();
                                removedCancellationTokenSource.Dispose();
                            }
                            catch
                            {
                                // Allow this to happen silently
                            }
                        });
#pragma warning restore CS4014
                    return wasAutoResetEventRemoved && wasCancellationTokenSourceRemoved;
                }
            }
            finally
            {
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
            }
        }

        /// <summary>
        /// Tries to get a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was found; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual bool TryGet(TKey key, out TValue value, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            DateTime? expired = null;
            var expiredValue = default(TValue);
            try
            {
                using (GetAsyncReaderWriterLock(key).ReaderLock(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!buckets.TryGetValue(key, out var bucket))
                    {
                        value = default;
                        return false;
                    }
                    if (bucket.Expiration < DateTime.UtcNow)
                    {
                        buckets.TryRemove(key, out var removedBucket);
                        expired = removedBucket.Expiration;
                        removedBucket.TrySoftGetValue(out expiredValue);
                        value = default;
                        return false;
                    }
                    value = bucket.GetValue(cancellationToken);
                    return true;
                }
            }
            finally
            {
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
            }
        }

        /// <summary>
        /// Tries to get a value in the cache
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was found; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public bool TryGet<T>(TKey key, out T value, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            var val = default(TValue);
            try
            {
                return TryGet(key, out val, cancellationToken) && val is T;
            }
            finally
            {
                value = (T)val;
            }
        }

        /// <summary>
        /// Tries to get a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>A <see cref="TryGetResult"/> indicating whether the value was found, and if so, what it is</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual async Task<TryGetResult> TryGetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            DateTime? expired = null;
            var expiredValue = default(TValue);
            try
            {
                using (await GetAsyncReaderWriterLock(key).ReaderLockAsync(cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!buckets.TryGetValue(key, out var bucket))
                        return new TryGetResult();
                    if (bucket.Expiration < DateTime.UtcNow)
                    {
                        buckets.TryRemove(key, out var removedBucket);
                        expired = removedBucket.Expiration;
                        removedBucket.TrySoftGetValue(out expiredValue);
                        return new TryGetResult();
                    }
                    return new TryGetResult(await bucket.GetValueAsync(cancellationToken).ConfigureAwait(false));
                }
            }
            finally
            {
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
            }
        }

        /// <summary>
        /// Tries to get a value in the cache
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>A <see cref="TryGetResult{T}"/> indicating whether the value was found, and if so, what it is</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task<TryGetResult<T>> TryGetAsync<T>(TKey key, CancellationToken cancellationToken = default) where T : TValue
        {
            ThrowIfDisposed();
            var tgr = await TryGetAsync(key, cancellationToken);
            if (tgr.WasFound && tgr.Value is T value)
                return new TryGetResult<T>(value);
            return new TryGetResult<T>();
        }

        /// <summary>
        /// Tries to remove a value from the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was removed; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual bool TryRemove(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            DateTime? expired = null;
            var expiredValue = default(TValue);
            try
            {
                TValue value;
                using (GetAsyncReaderWriterLock(key).WriterLock(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!buckets.TryGetValue(key, out var bucket))
                        return false;
                    refreshAutoResetEvents.TryRemove(key, out var removedAutoResetEvent);
                    if (refreshCancellationTokenSources.TryRemove(key, out var removedCancellationTokenSource))
                        Task.Run(() =>
                        {
                            try
                            {
                                removedCancellationTokenSource.Cancel();
                                removedCancellationTokenSource.Dispose();
                            }
                            catch
                            {
                                // Allow this to happen silently
                            }
                        });
                    var isExpired = bucket.Expiration < DateTime.UtcNow;
                    buckets.TryRemove(key, out var removedBucket);
                    if (isExpired)
                    {
                        expired = bucket.Expiration.Value;
                        bucket.TrySoftGetValue(out expiredValue);
                        return false;
                    }
                    removedBucket.TrySoftGetValue(out value);
                }
                OnValueRemoved(new KeyValueEventArgs(key, value));
                return true;
            }
            finally
            {
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
            }
        }

        /// <summary>
        /// Tries to remove a value from the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was removed; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual async Task<bool> TryRemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            DateTime? expired = null;
            var expiredValue = default(TValue);
            try
            {
                TValue value;
                using (await GetAsyncReaderWriterLock(key).WriterLockAsync(cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!buckets.TryGetValue(key, out var bucket))
                        return false;
                    refreshAutoResetEvents.TryRemove(key, out var removedAutoResetEvent);
                    if (refreshCancellationTokenSources.TryRemove(key, out var removedCancellationTokenSource))
#pragma warning disable CS4014
                        Task.Run(() =>
                        {
                            try
                            {
                                removedCancellationTokenSource.Cancel();
                                removedCancellationTokenSource.Dispose();
                            }
                            catch
                            {
                                // Allow this to happen silently
                            }
                        });
#pragma warning restore CS4014
                    var isExpired = bucket.Expiration < DateTime.UtcNow;
                    buckets.TryRemove(key, out var removedBucket);
                    if (isExpired)
                    {
                        expired = bucket.Expiration.Value;
                        bucket.TrySoftGetValue(out expiredValue);
                        return false;
                    }
                    removedBucket.TrySoftGetValue(out value);
                }
                OnValueRemoved(new KeyValueEventArgs(key, value));
                return true;
            }
            finally
            {
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
            }
        }

        /// <summary>
        /// Tries to signal refreshing a value
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if a signal was successfully sent to refresh the value; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual bool TrySignalRefresh(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            DateTime? expired = null;
            var expiredValue = default(TValue);
            try
            {
                using (GetAsyncReaderWriterLock(key).ReaderLock(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!buckets.TryGetValue(key, out var bucket))
                        return false;
                    if (bucket.Expiration < DateTime.UtcNow)
                    {
                        buckets.TryRemove(key, out var removedBucket);
                        expired = removedBucket.Expiration;
                        removedBucket.TrySoftGetValue(out expiredValue);
                        return false;
                    }
                    if (!refreshAutoResetEvents.TryGetValue(key, out var autoResetEvent))
                        return false;
                    autoResetEvent.Set();
                    return true;
                }
            }
            finally
            {
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
            }
        }

        /// <summary>
        /// Tries to signal refreshing a value
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if a signal was successfully sent to refresh the value; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public virtual async Task<bool> TrySignalRefreshAsync(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            DateTime? expired = null;
            var expiredValue = default(TValue);
            try
            {
                using (await GetAsyncReaderWriterLock(key).ReaderLockAsync(cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!buckets.TryGetValue(key, out var bucket))
                        return false;
                    if (bucket.Expiration < DateTime.UtcNow)
                    {
                        buckets.TryRemove(key, out var removedBucket);
                        expired = removedBucket.Expiration;
                        removedBucket.TrySoftGetValue(out expiredValue);
                        return false;
                    }
                    if (!refreshAutoResetEvents.TryGetValue(key, out var autoResetEvent))
                        return false;
                    autoResetEvent.Set();
                    return true;
                }
            }
            finally
            {
                if (expired != null)
                    OnValueExpired(new ValueExpiredEventArgs(key, expiredValue, expired.Value));
            }
        }

        /// <summary>
        /// Tries to update a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TryOperationResult TryUpdate(TKey key, TValue value, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdate(key, ValueSource<TValue>.Create(value), false, null, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TryOperationResult TryUpdate(TKey key, TValue value, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdate(key, ValueSource<TValue>.Create(value), true, expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TryOperationResult TryUpdate(TKey key, Func<TValue> valueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdate(key, ValueSource<TValue>.Create(valueFactory), false, null, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TryOperationResult TryUpdate(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdate(key, ValueSource<TValue>.Create(cancelableValueFactory), false, null, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TryOperationResult TryUpdate(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdate(key, ValueSource<TValue>.Create(valueFactory), true, expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public TryOperationResult TryUpdate(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdate(key, ValueSource<TValue>.Create(cancelableValueFactory), true, expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryUpdateAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdateAsync(key, ValueSource<TValue>.Create(value), false, null, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryUpdateAsync(TKey key, TValue value, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdateAsync(key, ValueSource<TValue>.Create(value), true, expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryUpdateAsync(TKey key, Func<TValue> valueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdateAsync(key, ValueSource<TValue>.Create(valueFactory), false, null, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryUpdateAsync(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdateAsync(key, ValueSource<TValue>.Create(cancelableValueFactory), false, null, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryUpdateAsync(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdateAsync(key, ValueSource<TValue>.Create(valueFactory), true, expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryUpdateAsync(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdateAsync(key, ValueSource<TValue>.Create(cancelableValueFactory), true, expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryUpdateAsync(TKey key, Func<Task<TValue>> asyncValueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdateAsync(key, ValueSource<TValue>.Create(asyncValueFactory), false, null, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryUpdateAsync(TKey key, Func<CancellationToken, Task<TValue>> cancelableAsyncValueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdateAsync(key, ValueSource<TValue>.Create(cancelableAsyncValueFactory), false, null, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryUpdateAsync(TKey key, Func<Task<TValue>> asyncValueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdateAsync(key, ValueSource<TValue>.Create(asyncValueFactory), true, expireIn, cancellationToken);
        }

        /// <summary>
        /// Tries to update a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <returns>true if the value was updated; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public Task<TryOperationResult> TryUpdateAsync(TKey key, Func<CancellationToken, Task<TValue>> cancelableAsyncValueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return PerformTryUpdateAsync(key, ValueSource<TValue>.Create(cancelableAsyncValueFactory), true, expireIn, cancellationToken);
        }

        /// <summary>
        /// Updates a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void Update(TKey key, TValue value, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = TryUpdate(key, value, cancellationToken);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(value), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void Update(TKey key, TValue value, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = TryUpdate(key, value, expireIn, cancellationToken);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(value), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void Update(TKey key, Func<TValue> valueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = TryUpdate(key, valueFactory, cancellationToken);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(valueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void Update(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = TryUpdate(key, cancelableValueFactory, cancellationToken);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void Update(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = TryUpdate(key, valueFactory, expireIn, cancellationToken);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(valueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public void Update(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = TryUpdate(key, cancelableValueFactory, expireIn, cancellationToken);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task UpdateAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await TryUpdateAsync(key, value, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(value), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task UpdateAsync(TKey key, TValue value, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await TryUpdateAsync(key, value, expireIn, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(value), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task UpdateAsync(TKey key, Func<TValue> valueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await TryUpdateAsync(key, valueFactory, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(valueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task UpdateAsync(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await TryUpdateAsync(key, cancelableValueFactory, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="valueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task UpdateAsync(TKey key, Func<TValue> valueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await TryUpdateAsync(key, valueFactory, expireIn, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(valueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task UpdateAsync(TKey key, Func<CancellationToken, TValue> cancelableValueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await TryUpdateAsync(key, cancelableValueFactory, expireIn, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task UpdateAsync(TKey key, Func<Task<TValue>> asyncValueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await TryUpdateAsync(key, asyncValueFactory, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(asyncValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache without changing the existing expiration
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task UpdateAsync(TKey key, Func<CancellationToken, Task<TValue>> cancelableAsyncValueFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await TryUpdateAsync(key, cancelableAsyncValueFactory, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableAsyncValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="asyncValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task UpdateAsync(TKey key, Func<Task<TValue>> asyncValueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await TryUpdateAsync(key, asyncValueFactory, expireIn, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(asyncValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Updates a value in the cache
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="cancelableAsyncValueFactory">A factory to produce the value</param>
        /// <param name="expireIn">The amount of time in which the value should expire</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation</param>
		/// <exception cref="KeyNotFoundException">A value for <paramref name="key"/> was not found</exception>
        /// <exception cref="ObjectDisposedException">The cache has been disposed</exception>
        public async Task UpdateAsync(TKey key, Func<CancellationToken, Task<TValue>> cancelableAsyncValueFactory, TimeSpan? expireIn, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var result = await TryUpdateAsync(key, cancelableAsyncValueFactory, expireIn, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case TryOperationStatus.KeyNotFound:
                    throw new KeyNotFoundException();
                case TryOperationStatus.ValueSourceThrew:
                    throw new ArgumentException("The value source threw an exception", nameof(cancelableAsyncValueFactory), result.Exception);
            }
        }

        /// <summary>
        /// Ensures that a method representing an operation times out in the <see cref="OperationTimeout"/> property has been set
        /// </summary>
        /// <param name="method">The method</param>
        /// <param name="cancellationToken">The cancellation token from the caller</param>
        protected void WrapTimeout(Action<CancellationToken> method, CancellationToken cancellationToken)
        {
            if (OperationTimeout is TimeSpan operationTimeout)
                using (var timeoutCts = new CancellationTokenSource(operationTimeout))
                using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                    method(combinedCts.Token);
            else
                method(cancellationToken);
        }

        /// <summary>
        /// Ensures that a method representing an operation times out in the <see cref="OperationTimeout"/> property has been set and returns its return
        /// </summary>
        /// <typeparam name="T">The return type of the method</typeparam>
        /// <param name="method">The method</param>
        /// <param name="cancellationToken">The cancellation token from the caller</param>
        protected T WrapTimeout<T>(Func<CancellationToken, T> method, CancellationToken cancellationToken)
        {
            if (OperationTimeout is TimeSpan operationTimeout)
                using (var timeoutCts = new CancellationTokenSource(operationTimeout))
                using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                    return method(combinedCts.Token);
            else
                return method(cancellationToken);
        }

        /// <summary>
        /// Ensures that an asynchronous method representing an operation times out in the <see cref="OperationTimeout"/> property has been set
        /// </summary>
        /// <param name="asyncMethod">The asynchronous method</param>
        /// <param name="cancellationToken">The cancellation token from the caller</param>
        protected async Task WrapTimeoutAsync(Func<CancellationToken, Task> asyncMethod, CancellationToken cancellationToken)
        {
            if (OperationTimeout is TimeSpan operationTimeout)
                using (var timeoutCts = new CancellationTokenSource(operationTimeout))
                using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                    await asyncMethod(combinedCts.Token).ConfigureAwait(false);
            else
                await asyncMethod(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Ensures that a asynchronous method representing an operation times out in the <see cref="OperationTimeout"/> property has been set and returns its return
        /// </summary>
        /// <typeparam name="T">The return type of the method</typeparam>
        /// <param name="asyncMethod">The asynchronous method</param>
        /// <param name="cancellationToken">The cancellation token from the caller</param>
        protected async Task<T> WrapTimeoutAsync<T>(Func<CancellationToken, Task<T>> asyncMethod, CancellationToken cancellationToken)
        {
            if (OperationTimeout is TimeSpan operationTimeout)
                using (var timeoutCts = new CancellationTokenSource(operationTimeout))
                using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                    return await asyncMethod(combinedCts.Token).ConfigureAwait(false);
            else
                return await asyncMethod(cancellationToken).ConfigureAwait(false);
        }

        class Bucket
        {
            public Bucket(AsyncCache<TKey, TValue> owner, TKey key, TValue value, ValueSource<TValue> valueSource, TimeSpan? expireIn = null)
            {
                Expiration = DateTime.UtcNow + expireIn;
                Id = Guid.NewGuid();
                this.key = key;
                this.owner = owner;
                if (!owner.AddReferencesAsWeak || owner.WeakenReferencesIn == default)
                {
                    strongValue = value;
                    if (owner.WeakenReferencesIn is TimeSpan weakenIn)
                    {
                        valueAccess = new AsyncLock();
                        weakenAt = DateTime.UtcNow + weakenIn;
                        ScheduleWeakening();
                    }
                }
                else
                {
                    valueAccess = new AsyncLock();
                    ValueSource = valueSource;
                    weakValue = new WeakReference<object>(value);
                }
            }

            readonly TKey key;
            readonly AsyncCache<TKey, TValue> owner;
            TValue strongValue;
            readonly AsyncLock valueAccess;
            DateTime? weakenAt;
            WeakReference<object> weakValue;

            public TValue GetValue(CancellationToken cancellationToken = default)
            {
                if (!owner.AddReferencesAsWeak && owner.WeakenReferencesIn == default)
                    return strongValue;
                using (valueAccess.Lock(cancellationToken))
                {
                    if (weakValue == default)
                    {
                        if (weakenAt != default)
                            weakenAt = DateTime.UtcNow + owner.WeakenReferencesIn.Value;
                        return strongValue;
                    }
                    if (weakValue.TryGetTarget(out var reference))
                        return (TValue)reference;
                    var valueSource = ValueSource;
                    weakValue = default;
                    strongValue = valueSource.IsAsync ? valueSource.GetValueAsync(cancellationToken).Result : valueSource.GetValue(cancellationToken);
                    weakenAt = DateTime.UtcNow + owner.WeakenReferencesIn.Value;
                    ScheduleWeakening();
                    owner.OnValueReconstructed(new KeyValueEventArgs(key, strongValue));
                    return strongValue;
                }
            }

            public async Task<TValue> GetValueAsync(CancellationToken cancellationToken = default)
            {
                if (!owner.AddReferencesAsWeak && owner.WeakenReferencesIn == default)
                    return strongValue;
                using (await valueAccess.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (weakValue == default)
                    {
                        if (weakenAt != default)
                            weakenAt = DateTime.UtcNow + owner.WeakenReferencesIn.Value;
                        return strongValue;
                    }
                    if (weakValue.TryGetTarget(out var reference))
                        return (TValue)reference;
                    var valueSource = ValueSource;
                    weakValue = default;
                    strongValue = valueSource.IsAsync ? await valueSource.GetValueAsync(cancellationToken).ConfigureAwait(false) : valueSource.GetValue(cancellationToken);
                    weakenAt = DateTime.UtcNow + owner.WeakenReferencesIn.Value;
                    ScheduleWeakening();
                    owner.OnValueReconstructed(new KeyValueEventArgs(key, strongValue));
                    return strongValue;
                }
            }

            void ScheduleWeakening() =>
                Task.Run(async () =>
                {
                    while (true)
                    {
                        if (weakenAt == default)
                            break;
                        await Task.Delay(weakenAt.Value - DateTime.UtcNow).ConfigureAwait(false);
                        using (await valueAccess.LockAsync().ConfigureAwait(false))
                        {
                            if (weakenAt == default)
                                break;
                            if (DateTime.UtcNow < (weakenAt ?? DateTime.UtcNow))
                                continue;
                            var currentValue = strongValue;
                            strongValue = default;
                            weakValue = new WeakReference<object>(currentValue);
                            weakenAt = default;
                            owner.OnValueReferenceWeakened(new KeyValueEventArgs(key, currentValue));
                            break;
                        }
                    }
                });

            public void SetValue(TValue value, bool strengthen, CancellationToken cancellationToken = default)
            {
                if (!owner.AddReferencesAsWeak && owner.WeakenReferencesIn == default)
                    strongValue = value;
                else
                    using (valueAccess.Lock(cancellationToken))
                    {
                        if (strengthen)
                        {
                            strongValue = value;
                            weakValue = default;
                            if (owner.WeakenReferencesIn is TimeSpan weakenIn)
                            {
                                weakenAt = DateTime.UtcNow + weakenIn;
                                ScheduleWeakening();
                            }
                        }
                        else if (weakValue == default)
                        {
                            strongValue = value;
                            if (owner.WeakenReferencesIn is TimeSpan weakenIn && weakenAt != default)
                                weakenAt = DateTime.UtcNow + weakenIn;
                        }
                        else
                            weakValue.SetTarget(value);
                    }
            }

            public async Task SetValueAsync(TValue value, bool strengthen, CancellationToken cancellationToken = default)
            {
                if (!owner.AddReferencesAsWeak && owner.WeakenReferencesIn == default)
                    strongValue = value;
                else
                    using (await valueAccess.LockAsync(cancellationToken).ConfigureAwait(false))
                    {
                        if (strengthen)
                        {
                            strongValue = value;
                            weakValue = default;
                            if (owner.WeakenReferencesIn is TimeSpan weakenIn)
                            {
                                weakenAt = DateTime.UtcNow + weakenIn;
                                ScheduleWeakening();
                            }
                        }
                        else if (weakValue == default)
                        {
                            strongValue = value;
                            if (owner.WeakenReferencesIn is TimeSpan weakenIn && weakenAt != default)
                                weakenAt = DateTime.UtcNow + weakenIn;
                        }
                        else
                            weakValue.SetTarget(value);
                    }
            }

            public bool TrySoftGetValue(out TValue value)
            {
                if (!owner.AddReferencesAsWeak && owner.WeakenReferencesIn == default)
                {
                    value = strongValue;
                    return true;
                }
                using (valueAccess.Lock())
                {
                    if (weakValue == default)
                    {
                        value = strongValue;
                        return true;
                    }
                    if (weakValue.TryGetTarget(out var livingReference))
                    {
                        value = (TValue)livingReference;
                        return true;
                    }
                    value = default;
                    return false;
                }
            }

            public DateTime? Expiration { get; set; }
            public Guid Id { get; }
            public ValueSource<TValue> ValueSource { get; set; }
        }

        /// <summary>
        /// The source of a value to add or update in the cache
        /// </summary>
		protected abstract class ValueSource<T> where T : TValue
        {
			/// <summary>
			/// Creates a <see cref="ValueSource{T}"/> using an existing value
            /// </summary>
            /// <param name="value">The value</param>
            public static ValueSource<T> Create(T value) => new Value<T>(value);

            /// <summary>
			/// Creates a <see cref="ValueSource{T}"/> using a value factory
            /// </summary>
            /// <param name="valueFactory">The value factory</param>
            public static ValueSource<T> Create(Func<T> valueFactory) => new ValueFactory<T>(valueFactory);

            /// <summary>
			/// Creates a <see cref="ValueSource{T}"/> using a cancellable value factory
            /// </summary>
            /// <param name="cancelableValueFactory">The cancellable value factory</param>
            public static ValueSource<T> Create(Func<CancellationToken, T> cancelableValueFactory) => new CancelableValueFactory<T>(cancelableValueFactory);

            /// <summary>
			/// Creates a <see cref="ValueSource{T}"/> using an asynchronous value factory
            /// </summary>
            /// <param name="asyncValueFactory">The asynchronous value factory</param>
            public static ValueSource<T> Create(Func<Task<T>> asyncValueFactory) => new AsyncValueFactory<T>(asyncValueFactory);

            /// <summary>
			/// Creates a <see cref="ValueSource{T}"/> using a cancellable asynchronous value factory
            /// </summary>
			/// <param name="cancelableAsyncValueFactory">The cancellable asynchronous value factory</param>
            public static ValueSource<T> Create(Func<CancellationToken, Task<T>> cancelableAsyncValueFactory) => new AsyncCancelableValueFactory<T>(cancelableAsyncValueFactory);

            /// <summary>
            /// Creates a <see cref="ValueSource{TVariant}"/>
            /// </summary>
            /// <typeparam name="TVariant">The contravariant type</typeparam>
            public ValueSource<TVariant> CreateContravariantValueSource<TVariant>() where TVariant : TValue =>
                IsAsync ? ValueSource<TVariant>.Create(async cancellationToken => (TVariant)((object)await GetValueAsync(cancellationToken).ConfigureAwait(false))) : ValueSource<TVariant>.Create(cancellationToken => (TVariant)((object)GetValue(cancellationToken)));

            /// <summary>
            /// Gets the value
            /// </summary>
            /// <param name="cancellationToken">A cancellation token to request cancelling the production of the value</param>
			/// <exception cref="InvalidOperationException">The value source is asynchronous</exception>
			/// <exception cref="NotImplementedException">The value source failed to provide a synchronous implementation</exception>
            public virtual T GetValue(CancellationToken cancellationToken)
            {
                if (IsAsync)
                    throw new InvalidOperationException();
                throw new NotImplementedException();
            }

            /// <summary>
            /// Gets the value asynchronously
			/// </summary>
            /// <param name="cancellationToken">A cancellation token to request cancelling the production of the value</param>
            /// <exception cref="InvalidOperationException">The value source is synchronous</exception>
            /// <exception cref="NotImplementedException">The value source failed to provide an asynchronous implementation</exception>
            public virtual Task<T> GetValueAsync(CancellationToken cancellationToken)
            {
                if (!IsAsync)
                    throw new InvalidOperationException();
                throw new NotImplementedException();
            }

			/// <summary>
			/// Gets whether the value source is asynchronous
			/// </summary>
			public virtual bool IsAsync => false;
        };

        /// <summary>
		/// A <see cref="ValueSource{T}"/> created using an existing value
        /// </summary>
        protected sealed class Value<T> : ValueSource<T> where T : TValue
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="Value{T}"/> class
            /// </summary>
            /// <param name="value">The value</param>
            public Value(T value) => this.value = value;

            readonly T value;

			/// <summary>
            /// Gets the value
            /// </summary>
            /// <param name="cancellationToken">A cancellation token to request cancelling the production of the value</param>
            /// <exception cref="InvalidOperationException">The value source is asynchronous</exception>
            /// <exception cref="NotImplementedException">The value source failed to provide a synchronous implementation</exception>
            public override T GetValue(CancellationToken cancellationToken) => value;
        };

        /// <summary>
		/// A <see cref="ValueSource{T}"/> created using a value factory
        /// </summary>
        protected sealed class ValueFactory<T> : ValueSource<T> where T : TValue
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="ValueSource{T}"/> class
            /// </summary>
            /// <param name="valueFactory">The value factory</param>
            public ValueFactory(Func<T> valueFactory) => this.valueFactory = valueFactory;

            readonly Func<T> valueFactory;

			/// <summary>
            /// Gets the value
            /// </summary>
            /// <param name="cancellationToken">A cancellation token to request cancelling the production of the value</param>
            /// <exception cref="InvalidOperationException">The value source is asynchronous</exception>
            /// <exception cref="NotImplementedException">The value source failed to provide a synchronous implementation</exception>
            public override T GetValue(CancellationToken cancellationToken) => valueFactory();
        };

        /// <summary>
		/// A <see cref="ValueSource{T}"/> created using a cancelable value factory
        /// </summary>
        protected sealed class CancelableValueFactory<T> : ValueSource<T> where T : TValue
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="CancelableValueFactory{T}"/> class
            /// </summary>
            /// <param name="cancelableValueFactory">The cancelable value factory</param>
            public CancelableValueFactory(Func<CancellationToken, T> cancelableValueFactory) => this.cancelableValueFactory = cancelableValueFactory;

            readonly Func<CancellationToken, T> cancelableValueFactory;

			/// <summary>
            /// Gets the value
            /// </summary>
            /// <param name="cancellationToken">A cancellation token to request cancelling the production of the value</param>
            /// <exception cref="InvalidOperationException">The value source is asynchronous</exception>
            /// <exception cref="NotImplementedException">The value source failed to provide a synchronous implementation</exception>
            public override T GetValue(CancellationToken cancellationToken) => cancelableValueFactory(cancellationToken);
        };

        /// <summary>
		/// A <see cref="ValueSource{T}"/> created using an asynchronous value factory
        /// </summary>
        protected sealed class AsyncValueFactory<T> : ValueSource<T> where T : TValue
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="AsyncValueFactory{T}"/> class
            /// </summary>
            /// <param name="asyncValueFactory">The asynchronous value factory</param>
            public AsyncValueFactory(Func<Task<T>> asyncValueFactory) => this.asyncValueFactory = asyncValueFactory;

            readonly Func<Task<T>> asyncValueFactory;

			/// <summary>
            /// Gets the value asynchronously
            /// </summary>
            /// <param name="cancellationToken">A cancellation token to request cancelling the production of the value</param>
            /// <exception cref="InvalidOperationException">The value source is synchronous</exception>
            /// <exception cref="NotImplementedException">The value source failed to provide an asynchronous implementation</exception>
            public override Task<T> GetValueAsync(CancellationToken cancellationToken) => asyncValueFactory();

			/// <summary>
			/// Gets whether the value source is asynchronous
			/// </summary>
			public override bool IsAsync => true;
        };

        /// <summary>
		/// A <see cref="ValueSource{T}"/> created using a cancelable asynchronous value factory
        /// </summary>
        protected sealed class AsyncCancelableValueFactory<T> : ValueSource<T> where T : TValue
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="AsyncCancelableValueFactory{T}"/> class
            /// </summary>
            /// <param name="cancelableAsyncValueFactory">The cancelable async value factory</param>
            public AsyncCancelableValueFactory(Func<CancellationToken, Task<T>> cancelableAsyncValueFactory) => this.cancelableAsyncValueFactory = cancelableAsyncValueFactory;

            readonly Func<CancellationToken, Task<T>> cancelableAsyncValueFactory;

			/// <summary>
            /// Gets the value asynchronously
            /// </summary>
            /// <param name="cancellationToken">A cancellation token to request cancelling the production of the value</param>
            /// <exception cref="InvalidOperationException">The value source is synchronous</exception>
            /// <exception cref="NotImplementedException">The value source failed to provide an asynchronous implementation</exception>
            public override Task<T> GetValueAsync(CancellationToken cancellationToken) => cancelableAsyncValueFactory(cancellationToken);

			/// <summary>
			/// Gets whether the value source is asynchronous
			/// </summary>
			public override bool IsAsync => true;
        };
    }
}
