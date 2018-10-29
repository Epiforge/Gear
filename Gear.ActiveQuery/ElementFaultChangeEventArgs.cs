using System;

namespace Gear.ActiveQuery
{
    public class ElementFaultChangeEventArgs : EventArgs
    {
        public ElementFaultChangeEventArgs(object element, Exception fault)
        {
            Element = element;
            Fault = fault;
        }

        public ElementFaultChangeEventArgs(object element, Exception fault, int count) : this(element, fault) => Count = count;

        public int Count { get; }
        public object Element { get; }
        public Exception Fault { get; }
    }
}
