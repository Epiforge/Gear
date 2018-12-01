using Gear.Components;
using System.Linq;
using System.Threading;

namespace Gear.ActiveExpressions.Tests
{
    class TestPerson : PropertyChangeNotifier
    {
        public static TestPerson CreateEmily() => new TestPerson { name = "Emily" };

        public static TestPerson CreateJohn() => new TestPerson { name = "John" };

        public static TestPerson operator +(TestPerson a, TestPerson b) => new TestPerson { name = $"{a.name} {b.name}" };

        public static TestPerson operator -(TestPerson testPerson) => new TestPerson { name = new string(testPerson.name.Reverse().ToArray()) };

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
