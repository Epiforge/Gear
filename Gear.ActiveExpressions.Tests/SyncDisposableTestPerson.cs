using Gear.Components;
using System;
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
            throwOnDispose = a.throwOnDispose || b.throwOnDispose
        };

        public static SyncDisposableTestPerson operator -(SyncDisposableTestPerson syncDisposableTestPerson) => new SyncDisposableTestPerson
        {
            name = new string(syncDisposableTestPerson.name.Reverse().ToArray()),
            throwOnDispose = syncDisposableTestPerson.throwOnDispose
        };

        string name;
        long nameGets;
        bool throwOnDispose;

        protected override void Dispose(bool disposing)
        {
            if (throwOnDispose)
                throw new Exception("Throwing like I'm s'posed to!");
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
            set => SetBackedProperty(ref name, value);
        }

        public long NameGets => Interlocked.Read(ref nameGets);

        public bool ThrowOnDispose
        {
            get => throwOnDispose;
            set => SetBackedProperty(ref throwOnDispose, value);
        }
    }
}
