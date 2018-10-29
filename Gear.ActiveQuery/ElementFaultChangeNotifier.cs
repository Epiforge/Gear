using System;
using System.Collections.Generic;

namespace Gear.ActiveQuery
{
    class ElementFaultChangeNotifier : INotifyElementFaultChanges
    {
        public ElementFaultChangeNotifier(Func<IReadOnlyList<(object element, Exception fault)>> faultRetriever, out Action<ElementFaultChangeEventArgs> raiseElementFaultChanging, out Action<ElementFaultChangeEventArgs> raiseElementFaultChanged)
        {
            this.faultRetriever = faultRetriever;
            raiseElementFaultChanging = OnElementFaultChanging;
            raiseElementFaultChanged = OnElementFaultChanged;
        }

        public ElementFaultChangeNotifier(Func<IReadOnlyList<(object element, Exception fault)>> faultRetriever, out Action<object, Exception, int> raiseElementFaultChanging, out Action<object, Exception, int> raiseElementFaultChanged)
        {
            this.faultRetriever = faultRetriever;
            raiseElementFaultChanging = OnElementFaultChanging;
            raiseElementFaultChanged = OnElementFaultChanged;
        }

        readonly Func<IReadOnlyList<(object element, Exception fault)>> faultRetriever;

        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanged;
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanging;

        public IReadOnlyList<(object element, Exception fault)> GetElementFaults() => faultRetriever();

        protected virtual void OnElementFaultChanged(ElementFaultChangeEventArgs e) =>
            ElementFaultChanged?.Invoke(this, e);

        protected void OnElementFaultChanged(object element, Exception fault, int count) =>
            OnElementFaultChanged(new ElementFaultChangeEventArgs(element, fault, count));

        protected virtual void OnElementFaultChanging(ElementFaultChangeEventArgs e) =>
            ElementFaultChanging?.Invoke(this, e);

        protected void OnElementFaultChanging(object element, Exception fault, int count) =>
            OnElementFaultChanging(new ElementFaultChangeEventArgs(element, fault, count));
    }
}
