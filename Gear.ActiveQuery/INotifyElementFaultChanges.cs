using System;
using System.Collections.Generic;

namespace Gear.ActiveQuery
{
    public interface INotifyElementFaultChanges
    {
        event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanged;
        event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanging;

        IReadOnlyList<(object element, Exception fault)> GetElementFaults();
    }
}
