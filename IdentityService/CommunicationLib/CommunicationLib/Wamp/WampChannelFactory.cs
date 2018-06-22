using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using WampSharp.Core.Listener;
using WampSharp.V2;
using WampSharp.V2.Realm;

namespace Communication.Wamp
{
    public static class WampChannelFactory
    {
        public static IObservable<IWampChannel> CreateWampChannel(string url, string realm)
        {
            return Observable.Create<IWampChannel>(observer =>
            {
                DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();

                IWampChannel channel = channelFactory.CreateJsonChannel(url, realm);

                var realmProxy = channel.RealmProxy;

                EventHandler<WampSessionCreatedEventArgs> onEstablished = (e, w) =>
                {
                    Trace.WriteLine($"WAMP-channel opened. SessionId: {((WampSharp.V2.Client.WampSessionClient<Newtonsoft.Json.Linq.JToken>)channel.RealmProxy.Monitor).Session}");
                    observer.OnNext(channel);
                };
                EventHandler<WampConnectionErrorEventArgs> onFail = (e, w) =>
                {
                    Trace.WriteLine($"WAMP-channel in error. SessionId: {((WampSharp.V2.Client.WampSessionClient<Newtonsoft.Json.Linq.JToken>)channel.RealmProxy.Monitor).Session}. Error: {w.Exception.Message}");
                    observer.OnError(w.Exception);
                };
                EventHandler<WampSessionCloseEventArgs> onEnd = (e, w) =>
                {
                    Trace.WriteLine($"WAMP-channel completed. SessionId: {((WampSharp.V2.Client.WampSessionClient<Newtonsoft.Json.Linq.JToken>)channel.RealmProxy.Monitor).Session}. Reason: {w.Reason}");
                    observer.OnError(new Exception(w.Reason));
                };

                realmProxy.Monitor.ConnectionEstablished += onEstablished;
                realmProxy.Monitor.ConnectionError += onFail;
                realmProxy.Monitor.ConnectionBroken += onEnd;

                Trace.WriteLine("Opening WAMP-channel");
                channel.Open().ConfigureAwait(false);

                Action onClose = () =>
                {
                    Trace.WriteLine("Disconnecting WAMP-channel");
                    channel.Close();
                    realmProxy.Monitor.ConnectionEstablished -= onEstablished;
                    realmProxy.Monitor.ConnectionError -= onFail;
                    realmProxy.Monitor.ConnectionBroken -= onEnd;
                };

                return Disposable.Create(onClose);
            });
        }
    }
}
