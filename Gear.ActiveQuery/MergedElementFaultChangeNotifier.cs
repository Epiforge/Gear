using Gear.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gear.ActiveQuery
{
    class MergedElementFaultChangeNotifier : SyncDisposable, INotifyElementFaultChanges
    {
        public MergedElementFaultChangeNotifier(IEnumerable<INotifyElementFaultChanges> elementFaultChangeNotifiers)
        {
            this.elementFaultChangeNotifiers = elementFaultChangeNotifiers;
            foreach (var elementFaultChangeNotifier in this.elementFaultChangeNotifiers.Where(elementFaultChangeNotifier => elementFaultChangeNotifier != null))
            {
                elementFaultChangeNotifier.ElementFaultChanged += ElementFaultChangeNotifierElementFaultChanged;
                elementFaultChangeNotifier.ElementFaultChanging += ElementFaultChangeNotifierElementFaultChanging;
            }
        }

        public MergedElementFaultChangeNotifier(params INotifyElementFaultChanges[] elementFaultChangeNotifiers) : this((IEnumerable<INotifyElementFaultChanges>)elementFaultChangeNotifiers)
        {
        }

        IEnumerable<INotifyElementFaultChanges> elementFaultChangeNotifiers;

        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanged;
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanging;

        protected override void Dispose(bool disposing)
        {
            foreach (var elementFaultChangeNotifier in elementFaultChangeNotifiers.Where(elementFaultChangeNotifier => elementFaultChangeNotifier != null))
            {
                elementFaultChangeNotifier.ElementFaultChanged -= ElementFaultChangeNotifierElementFaultChanged;
                elementFaultChangeNotifier.ElementFaultChanging -= ElementFaultChangeNotifierElementFaultChanging;
            }
        }

        void ElementFaultChangeNotifierElementFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(sender, e);

        void ElementFaultChangeNotifierElementFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(sender, e);

        public IReadOnlyList<(object element, Exception fault)> GetElementFaults() =>
            elementFaultChangeNotifiers.SelectMany(elementFaultChangeNotifier => elementFaultChangeNotifier?.GetElementFaults() ?? Enumerable.Empty<(object element, Exception fault)>()).ToList();
    }
}
