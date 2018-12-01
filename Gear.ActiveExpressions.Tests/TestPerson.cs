using Gear.Components;
using System.Threading;

namespace Gear.ActiveExpressions.Tests
{
    class TestPerson : PropertyChangeNotifier
    {
        public static TestPerson CreateEmily() => new TestPerson { name = "Emily" };

        public static TestPerson CreateJohn() => new TestPerson { name = "John" };

        public static TestPerson operator +(TestPerson a, TestPerson b) => new TestPerson { name = $"{a.name} {b.name}" };

        string name;
        long nameGets;

        public override string ToString() => $"{{{name}}}";

        public string Name
        {
            get
            {
                OnPropertyChanging(nameof(NameGets));
                Interlocked.Increment(ref nameGets);
                OnPropertyChanged(nameof(NameGets));
                return name;
            }
            set => SetBackedProperty(ref name, value);
        }

        public long NameGets => Interlocked.Read(ref nameGets);
    }


}
