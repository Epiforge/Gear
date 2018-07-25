using System;

namespace Gear.ActiveQuery
{
    public class ElementPropertyChangeEventArgs<T> : EventArgs
    {
        public ElementPropertyChangeEventArgs(T element, string propertyName)
        {
            Element = element;
            PropertyName = propertyName;
        }

        public T Element { get; }
        public string PropertyName { get; }
    }
}
