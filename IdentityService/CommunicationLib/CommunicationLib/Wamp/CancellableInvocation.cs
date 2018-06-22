using System.Threading;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Rpc;

namespace Communication.Wamp
{

    internal class CancellableInvocation : IWampCancellableInvocation
    {
        private readonly CancellationTokenSource cancellationTokenSource;

        public CancellableInvocation(CancellationTokenSource cancellationTokenSource)
        {
            this.cancellationTokenSource = cancellationTokenSource;
        }

        public void Cancel(InterruptDetails details)
        {
            cancellationTokenSource.Cancel();
        }
    }
}
