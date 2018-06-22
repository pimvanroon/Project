using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SystemEx;
using WampSharp.V2;
using WampSharp.V2.Client;
using WampSharp.V2.Core.Contracts;

namespace Communication.Wamp
{
    /// <summary>
    /// Implements a WAMP-communication stack
    /// Use <c>Configure</c> to register the WAMP-methods for all supported versions
    /// </summary>
    internal class WampService : IWampService
    {
        private readonly MetadataCollection metadataColl;
        private readonly ILogger logger;
        private readonly IObservable<IWampChannel> channelObservable;

        private IDisposable channelSubscription;
        private IEnumerable<IWampOperation> operations;

        public WampService(MetadataCollection metadataColl, ILogger<WampService> logger,
            IObservable<IWampChannel> channelObservable)
        {
            this.metadataColl = metadataColl;
            this.logger = logger;
            this.channelObservable = channelObservable;
        }

        /// <summary>
        /// Register the commands of all services of all supported versions (using the <c>metadataColl</c>) at the WAMP-router
        /// </summary>
        /// <param name="provider">A service provider that provides a concrete service instance given its <c>Type</c> using Dependency Injection</param>
        /// <returns></returns>
        public void Configure(IServiceProvider provider)
        {
            // Build a collection of WAMP-operations
            this.operations =
                Enumerable.Range(metadataColl.MinVersion, metadataColl.MaxVersion - metadataColl.MinVersion + 1)
                    .Select(version => metadataColl[version])
                    .SelectMany(metadata => metadata.Services.Values, (m, s) => new { Metadata = m, Service = s })
                    .SelectMany(it => it.Service.Commands.Values, (it, command) =>
                    {
                        var operation = provider.GetService<IWampOperation>();
                        operation.Configure(it.Metadata, command);
                        return operation;
                    }
                    ).ToArray();
            logger.LogInformation($"Discovered {operations.Count()} operations");
        }

        public void Run()
        {
            List<Task<IAsyncDisposable>> registeredTasks = null;
            if (channelSubscription != null)
                throw new InvalidOperationException("WampService already started");

            channelSubscription = channelObservable.Subscribe(
                next =>
                    {
                        IWampRealmProxy realm = next.RealmProxy;
                        RegisterOptions registerOptions = new RegisterOptions
                        {
                            DiscloseCaller = true,
                            Invoke = WampInvokePolicy.Roundrobin,
                            Match = WampMatchPattern.Exact
                        };

                        // Register the wamp operations at the RpcCatalog on the router via the (newly) established channel
                        registeredTasks = new List<Task<IAsyncDisposable>>();
                        foreach (var wampOperation in operations)
                        {
                            var disposableTask = realm.RpcCatalog.Register(wampOperation, registerOptions);
                            registeredTasks.Add(disposableTask);
                        }

                        // Wait until all operations are registered
                        Task.WhenAll(registeredTasks)
                            .ContinueWith(t =>
                            {
                                if (t.Exception != null)
                                    logger.LogError(t.Exception.InnerException, $"Error registering {operations.Count()} operations at the WAMP-router");
                                else
                                    logger.LogInformation($"Registered {operations.Count()} operations at the WAMP-router");
                            }
                        );
                    },
                error =>
                    {
                        logger.LogError(error, $"Connection lost to WAMP-router");
                    },
                () =>       // Completed
                    {
                        var disposeTasks = new List<Task>();
                        foreach (var registeredRpc in registeredTasks)
                        {
                            disposeTasks.Add(registeredRpc.Result.DisposeAsync());
                        }
                        Task.WhenAll(disposeTasks)
                            .ContinueWith(t =>
                            {
                                if (t.Exception != null)
                                    logger.LogError(t.Exception.InnerException, $"Error unregistering {operations.Count()} operations at the WAMP-router");
                                else
                                    logger.LogInformation($"Unregistered {operations.Count()} operations at the WAMP-router");
                            }
                        );
                    }
            );
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Unregister();
        }

        public void Unregister()
        {
            if (channelSubscription != null)
            {
                channelSubscription.Dispose();
                channelSubscription = null;
            }
        }

    }
}
