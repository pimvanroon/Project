using Microsoft.AspNetCore.Builder;
using System;
using System.Threading.Tasks;

namespace Communication.Wamp
{
    /// <summary>
    /// An interface to a WAMP-communication stack
    /// </summary>
    public interface IWampService: IDisposable
    {
        void Configure(IServiceProvider provider);

        void Run();
    }
}
