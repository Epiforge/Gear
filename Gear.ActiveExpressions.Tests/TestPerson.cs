using Gear.Components;
using System;
using System.Threading;

namespace Gear.ActiveExpressions.Tests
{
    class TestPerson : PropertyChangeNotifier
    {
        public static TestPerson CreateEmily() => new TestPerson { Name = "Emily", Birth = new DateTime(1993, 10, 19) };

        public static TestPerson CreateJohn() => new TestPerson { Name = "John", Birth = new DateTime(1988, 2, 12) };

        DateTime birth;
        long birthGets;
        string name;
        long nameGets;

        public DateTime Birth
        {
            get
            {
                OnPropertyChanging(nameof(BirthGets));
                Interlocked.Increment(ref birthGets);
                OnPropertyChanged(nameof(BirthGets));
                return birth;
            }
            set => SetBackedProperty(ref birth, value);
        }

        public long BirthGets => Interlocked.Read(ref birthGets);

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

        public override string ToString() => "[TestPerson]";
    }
}
