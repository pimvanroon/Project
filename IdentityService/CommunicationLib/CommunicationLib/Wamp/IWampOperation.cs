using System;
using System.Collections.Generic;
using System.Text;
using WampSharp.V2.Rpc;

namespace Communication.Wamp
{
    internal interface IWampOperation : IWampRpcOperation
    {
        void Configure(Metadata metadata, Metadata.Command command);
    }
}
