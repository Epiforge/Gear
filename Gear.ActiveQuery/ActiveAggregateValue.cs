using Gear.Components;
using System;

namespace Gear.ActiveQuery
{
    public class ActiveAggregateValue<T> : DisposablePropertyChangeNotifier
    {
        public ActiveAggregateValue(bool isValid, T value, out Action<bool> setValidity, out Action<T> setValue, Action<bool> disposeAction = null)
        {
            this.isValid = isValid;
            this.value = value;
            setValidity = v => IsValid = v;
            setValue = v => Value = v;
            this.disposeAction = disposeAction;
        }

        Action<bool> disposeAction;
        bool isValid;
        T value;

        protected override void Dispose(bool disposing) => disposeAction?.Invoke(disposing);

        protected override bool IsDisposable => true;

        public bool IsValid
        {
            get => isValid;
            private set => SetBackedProperty(ref isValid, value);
        }

        public T Value
        {
            get => value;
            private set => SetBackedProperty(ref this.value, value);
        }
    }
}
