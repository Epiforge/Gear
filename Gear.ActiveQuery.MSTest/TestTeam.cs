using Gear.Components;
using System;
using System.Threading;

namespace Gear.ActiveQuery.MSTest
{
    class TestTeam : PropertyChangeNotifier, IComparable<TestTeam>
    {
        public TestTeam(SynchronizationContext synchronizationContext) : this(new SynchronizedRangeObservableCollection<TestPerson>(synchronizationContext))
        {
        }

        public TestTeam(SynchronizedRangeObservableCollection<TestPerson> people) => this.people = people;

        SynchronizedRangeObservableCollection<TestPerson> people;

        public int CompareTo(TestTeam other) => GetHashCode().CompareTo(other?.GetHashCode() ?? 0);

        public SynchronizedRangeObservableCollection<TestPerson> People
        {
            get => people;
            set => SetBackedProperty(ref people, in value);
        }
    }
}
