using System;

namespace Gear.ActiveQuery
{
    public class RangeActiveExpressionElementFaultException<TElement> : Exception
    {
        public RangeActiveExpressionElementFaultException(TElement element, Exception innerException) : base("An unhandled exception was thrown by an active expression", innerException) => Element = element;

        public TElement Element { get; }
    }
}
