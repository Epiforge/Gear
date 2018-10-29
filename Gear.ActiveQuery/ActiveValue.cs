using Gear.Components;
using System;
using System.Collections.Generic;

namespace Gear.ActiveQuery
{
    public class ActiveValue<TValue> : SyncDisposablePropertyChangeNotifier, INotifyElementFaultChanges
    {
        public ActiveValue(TValue value, Exception operationFault = null, INotifyElementFaultChanges elementFaultChangeNotifier = null)
        {
            this.value = value;
            this.operationFault = operationFault;
            this.elementFaultChangeNotifier = elementFaultChangeNotifier;
            InitializeFaultNotification();
        }

        public ActiveValue(out Action<TValue> setValue, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(default, out setValue, null, elementFaultChangeNotifier, onDispose)
        {
        }

        public ActiveValue(out Action<TValue> setValue, out Action<Exception> setOperationFault, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(default, out setValue, null, out setOperationFault, elementFaultChangeNotifier, onDispose)
        {
        }

        public ActiveValue(out Action<TValue> setValue, Exception operationFault, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(default, out setValue, operationFault, elementFaultChangeNotifier, onDispose)
        {
        }

        public ActiveValue(TValue value, out Action<TValue> setValue, Exception operationFault = null, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(value, operationFault, elementFaultChangeNotifier)
        {
            setValue = SetValue;
            this.onDispose = onDispose;
        }

        public ActiveValue(TValue value, out Action<TValue> setValue, out Action<Exception> setOperationFault, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(value, out setValue, null, elementFaultChangeNotifier, onDispose) =>
            setOperationFault = SetOperationFault;

        public ActiveValue(TValue value, out Action<TValue> setValue, Exception operationFault, out Action<Exception> setOperationFault, INotifyElementFaultChanges elementFaultChangeNotifier = null, Action onDispose = null) : this(value, out setValue, operationFault, elementFaultChangeNotifier, onDispose) =>
            setOperationFault = SetOperationFault;

        readonly INotifyElementFaultChanges elementFaultChangeNotifier;
        readonly Action onDispose;
        Exception operationFault;
        TValue value;

        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanged;
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanging;

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

        public Exception OperationFault
        {
            get => operationFault;
            private set => SetBackedProperty(ref operationFault, in value);
        }

        public TValue Value
        {
            get => value;
            private set => SetBackedProperty(ref this.value, in value);
        }
    }
}
