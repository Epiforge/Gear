using Gear.Components;
using System;
using System.Collections.Generic;

namespace Gear.ActiveQuery
{
    /// <summary>
    /// Represents the scalar result of an active query
    /// </summary>
    /// <typeparam name="TValue">The type of the scalar result</typeparam>
    public class ActiveValue<TValue> : SyncDisposablePropertyChangeNotifier, IActiveValue<TValue>
    {
        internal ActiveValue(TValue value, Exception operationFault = null, INotifyElementFaultChanges elementFaultChangeNotifier = null)
        {
            this.value = value;
            this.operationFault = operationFault;
            this.elementFaultChangeNotifier = elementFaultChangeNotifier;
            InitializeFaultNotification();
        }

        internal ActiveValue(out Action<TValue> setValue, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(default, out setValue, null, elementFaultChangeNotifier, onDispose)
        {
        }

        internal ActiveValue(out Action<TValue> setValue, out Action<Exception> setOperationFault, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(default, out setValue, null, out setOperationFault, elementFaultChangeNotifier, onDispose)
        {
        }

        internal ActiveValue(out Action<TValue> setValue, Exception operationFault, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(default, out setValue, operationFault, elementFaultChangeNotifier, onDispose)
        {
        }

        internal ActiveValue(TValue value, out Action<TValue> setValue, Exception operationFault = null, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(value, operationFault, elementFaultChangeNotifier)
        {
            setValue = SetValue;
            this.onDispose = onDispose;
        }

        internal ActiveValue(TValue value, out Action<TValue> setValue, out Action<Exception> setOperationFault, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(value, out setValue, null, elementFaultChangeNotifier, onDispose) =>
            setOperationFault = SetOperationFault;

        internal ActiveValue(TValue value, out Action<TValue> setValue, Exception operationFault, out Action<Exception> setOperationFault, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(value, out setValue, operationFault, elementFaultChangeNotifier, onDispose) =>
            setOperationFault = SetOperationFault;

        readonly INotifyElementFaultChanges elementFaultChangeNotifier;
        readonly Action onDispose;
        Exception operationFault;
        TValue value;

        /// <summary>
        /// Occurs when the fault for an element has changed
        /// </summary>
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanged;

        /// <summary>
        /// Occurs when the fault for an element is changing
        /// </summary>
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanging;

        /// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
        protected override void Dispose(bool disposing)
        {
            onDispose?.Invoke();
            if (elementFaultChangeNotifier != null)
            {
                elementFaultChangeNotifier.ElementFaultChanged -= ElementFaultChangeNotifierElementFaultChanged;
                elementFaultChangeNotifier.ElementFaultChanging -= ElementFaultChangeNotifierElementFaultChanging;
            }
        }

        void ElementFaultChangeNotifierElementFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(this, e);

        void ElementFaultChangeNotifierElementFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanging?.Invoke(this, e);

        /// <summary>
        /// Gets a list of all faulted elements
        /// </summary>
        /// <returns>The list</returns>
        public IReadOnlyList<(object element, Exception fault)> GetElementFaults() => elementFaultChangeNotifier?.GetElementFaults();

        void InitializeFaultNotification()
        {
            if (elementFaultChangeNotifier != null)
            {
                elementFaultChangeNotifier.ElementFaultChanged += ElementFaultChangeNotifierElementFaultChanged;
                elementFaultChangeNotifier.ElementFaultChanging += ElementFaultChangeNotifierElementFaultChanging;
            }
        }

        void SetOperationFault(Exception operationFault) => OperationFault = operationFault;

        void SetValue(TValue value) => Value = value;

        /// <summary>
        /// Gets the exception that occured the most recent time the query updated
        /// </summary>
        public Exception OperationFault
        {
            get => operationFault;
            private set => SetBackedProperty(ref operationFault, in value);
        }

        /// <summary>
        /// Gets the value from the most recent time the query updated
        /// </summary>
        public TValue Value
        {
            get => value;
            private set => SetBackedProperty(ref this.value, in value);
        }
    }
}
