using Gear.Components;
using System.Linq;
using System.Threading;

namespace Gear.ActiveQuery.MSTest
{
    class TestPerson : PropertyChangeNotifier
    {
        public static SynchronizedRangeObservableCollection<TestPerson> CreatePeople(SynchronizationContext synchronizationContext = null) =>
            new SynchronizedRangeObservableCollection<TestPerson>(synchronizationContext ?? new TestSynchronizationContext(0))
            {
                new TestPerson("John"),
                new TestPerson("Emily"),
                new TestPerson("Charles"),
                new TestPerson("Erin"),
                new TestPerson("Cliff"),
                new TestPerson("Hunter"),
                new TestPerson("Ben"),
                new TestPerson("Craig"),
                new TestPerson("Bridget"),
                new TestPerson("Nanette"),
                new TestPerson("George"),
                new TestPerson("Bryan"),
                new TestPerson("James"),
                new TestPerson("Steve")
            };

        public static TestPerson operator +(TestPerson a, TestPerson b) => new TestPerson { name = $"{a.name} {b.name}" };

        public static TestPerson operator -(TestPerson testPerson) => new TestPerson { name = new string(testPerson.name.Reverse().ToArray()) };

        public TestPerson()
        {
        }

        public TestPerson(string name) => this.name = name;

        string name;

        public string Name
        {
            get => name;
            set => SetBackedProperty(ref name, in value);
        }
    }
}
