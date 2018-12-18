using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.ActiveQuery.MSTest
{
    sealed class TestSynchronizationContext : SynchronizationContext
    {
        public TestSynchronizationContext(int delayInMilliseconds = 0)
        {
            if (delayInMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(delayInMilliseconds));
            this.delayInMilliseconds = delayInMilliseconds;
        }

        readonly int delayInMilliseconds;

        public override void Post(SendOrPostCallback d, object state)
        {
            Task.Run(async () =>
            {
                if (delayInMilliseconds > 0)
                    await Task.Delay(delayInMilliseconds).ConfigureAwait(false);
                d(state);
            });
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (delayInMilliseconds > 0)
                Thread.Sleep(delayInMilliseconds);
            d(state);
        }
    }
}
