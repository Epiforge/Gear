using Gear.Components;
using System.Linq;
using System.Threading;

namespace Gear.ActiveExpressions.Tests
{
    class SyncDisposableTestPerson : SyncDisposablePropertyChangeNotifier
    {
        public static SyncDisposableTestPerson CreateEmily() => new SyncDisposableTestPerson { name = "Emily" };

        public static SyncDisposableTestPerson CreateJohn() => new SyncDisposableTestPerson { name = "John" };

        public static SyncDisposableTestPerson operator +(SyncDisposableTestPerson a, SyncDisposableTestPerson b) => new SyncDisposableTestPerson
        {
            name = $"{a.name} {b.name}",
        };

        public static SyncDisposableTestPerson operator -(SyncDisposableTestPerson syncDisposableTestPerson) => new SyncDisposableTestPerson
        {
            name = new string(syncDisposableTestPerson.name.Reverse().ToArray()),
        };

        public SyncDisposableTestPerson()
        {
        }

        public SyncDisposableTestPerson(string name) => this.name = name;

        string name;
        long nameGets;

        protected override void Dispose(bool disposing)
        {
        }

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
            set => SetBackedProperty(ref name, in value);
        }

        public long NameGets => Interlocked.Read(ref nameGets);
    }
}
