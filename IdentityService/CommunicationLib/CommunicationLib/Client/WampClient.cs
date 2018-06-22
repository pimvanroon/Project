using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using WampSharp.V2;
using WampSharp.V2.Client;
using WampSharp.V2.Fluent;

namespace Communication.Client
{
    public class PcaWampClient
    {
        private readonly IWampChannel channel;

        public PcaWampClient(Uri address, string realm, TypeMapper typeMapper)
            : this(address, realm, null, typeMapper)
        {
        }

        public PcaWampClient(Uri address, string realm, IWampClientAuthenticator authenticator, TypeMapper typeMapper)
        {
            var serializer = new Newtonsoft.Json.JsonSerializer();
            if (typeMapper != null)
                serializer.Converters.Add(new DerivedEntityJsonConverter(typeMapper));

            var builder =
               new WampChannelFactory().ConnectToRealm(realm)
                      .WebSocketTransport(address)
                      .JsonSerialization(serializer);
            this.channel = authenticator != null ?
                builder.Authenticator(authenticator).Build() :
                builder.Build();
        }

        public IProxy StartClient<IProxy>() where IProxy : class
        {
            channel.Open().Wait(5000);
            return channel.RealmProxy.Services.GetCalleeProxy<IProxy>();
        }

        // Helper class to support ObservableFromProgressiveResult
        // Converts IProgress<T> interface into a delegate
        private class ProgressDelegate<T> : IProgress<T>
        {
            private readonly Action<T> report;

            public ProgressDelegate(Action<T> report)
            {
                this.report = report;
            }

            public void Report(T value)
            {
                report(value);
            }
        }

        /// <summary>
        /// Implements a <c>[WampProgressiveResultProcedure]</c> as an observable method
        /// </summary>
        /// <typeparam name="T">The result type of the Observable</typeparam>
        /// <param name="function">The method that follows the <c>[WampProgressiveResultProcedure]</c>-pattern.</param>
        /// <returns></returns>
        public static IObservable<T> ObservableFromProgressiveResult<T>(Func<IProgress<T>, CancellationToken, Task<T>> function)
        {
            return Observable.Create<T>(obs =>
            {
                var cancellationSource = new CancellationTokenSource();
                var progress = new ProgressDelegate<T>(obs.OnNext);
                var task = function(progress, cancellationSource.Token);
                task.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                        obs.OnCompleted();
                    else
                        obs.OnError(t.Exception.InnerException);
                });
                return Disposable.Create(() => cancellationSource.Cancel());
            });
        }
    }
}
