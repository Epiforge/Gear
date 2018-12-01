using Gear.Components;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.ActiveExpressions.Tests
{
    class AsyncDisposableTestPerson : AsyncDisposablePropertyChangeNotifier
    {
        public static AsyncDisposableTestPerson CreateEmily() => new AsyncDisposableTestPerson { name = "Emily" };

        public static AsyncDisposableTestPerson CreateJohn() => new AsyncDisposableTestPerson { name = "John" };

        public static AsyncDisposableTestPerson operator +(AsyncDisposableTestPerson a, AsyncDisposableTestPerson b) => new AsyncDisposableTestPerson { name = $"{a.name} {b.name}" };

        public static AsyncDisposableTestPerson operator -(AsyncDisposableTestPerson asyncDisposableTestPerson) => new AsyncDisposableTestPerson { name = new string(asyncDisposableTestPerson.name.Reverse().ToArray()) };

        string name;
        long nameGets;

        protected override Task DisposeAsync(bool disposing, CancellationToken cancellationToken = default(CancellationToken)) => Task.CompletedTask;

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
