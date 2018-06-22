using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using Communication;
using Communication.Wamp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SystemEx;
using WampSharp.Core.Serialization;
using WampSharp.V2;
using WampSharp.V2.Client;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Rpc;
using Xunit;

namespace Communication.Tests
{
    public class TestWamp
    {
        private class MockRawRpcOperationClientCallback : IWampRawRpcOperationClientCallback
        {
            public object Message { get; set; }

            public void Error<TMessage>(IWampFormatter<TMessage> formatter, TMessage details, string error) => throw new NotImplementedException();

            public void Error<TMessage>(IWampFormatter<TMessage> formatter, TMessage details, string error, TMessage[] arguments) => throw new NotImplementedException();

            public void Error<TMessage>(IWampFormatter<TMessage> formatter, TMessage details, string error, TMessage[] arguments, TMessage argumentsKeywords) => throw new NotImplementedException();

            public void Result<TMessage>(IWampFormatter<TMessage> formatter, ResultDetails details)
            {
                Message = null;
            }

            public void Result<TMessage>(IWampFormatter<TMessage> formatter, ResultDetails details, TMessage[] arguments)
            {
                Message = arguments != null && arguments.Length > 0 ? arguments[0] : default(TMessage);
            }

            public void Result<TMessage>(IWampFormatter<TMessage> formatter, ResultDetails details, TMessage[] arguments, IDictionary<string, TMessage> argumentsKeywords) => throw new NotImplementedException();
        }

        private class MockRpcOperationRouterCallback : IWampRawRpcOperationRouterCallback
        {
            private readonly IWampRawRpcOperationClientCallback caller;

            public MockRpcOperationRouterCallback(IWampRawRpcOperationClientCallback caller)
            {
                this.caller = caller;
            }

            public void Error<TMessage>(IWampFormatter<TMessage> formatter, TMessage details, string error) => throw new NotImplementedException();

            public void Error<TMessage>(IWampFormatter<TMessage> formatter, TMessage details, string error, TMessage[] arguments) => throw new NotImplementedException();

            public void Error<TMessage>(IWampFormatter<TMessage> formatter, TMessage details, string error, TMessage[] arguments, TMessage argumentsKeywords) => throw new NotImplementedException();

            public void Result<TMessage>(IWampFormatter<TMessage> formatter, YieldOptions details)
            {
                caller.Result(formatter, new ResultDetails());
            }

            public void Result<TMessage>(IWampFormatter<TMessage> formatter, YieldOptions details, TMessage[] arguments)
            {
                caller.Result(formatter, new ResultDetails(), arguments);
            }

            public void Result<TMessage>(IWampFormatter<TMessage> formatter, YieldOptions details, TMessage[] arguments, IDictionary<string, TMessage> argumentsKeywords) => throw new NotImplementedException();
        }

        private class MockLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state)
            {
                return Disposable.Empty;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Trace.WriteLine($"{logLevel}: {formatter(state, exception)}");
            }
        }

        [Fact]
        public void Wamp_CanCallCommand()
        {
            // Local variable where the registered procedures are stored
            var procedures = new Dictionary<string, IWampRpcOperation>();

            var rpcOperationClientCallback = new MockRawRpcOperationClientCallback();
            var rpcOperationRouterCallbackMock = new MockRpcOperationRouterCallback(rpcOperationClientCallback);

            // Setup empty IAsyncDisposable
            var emptyAsyncDisposableMock = new Mock<IAsyncDisposable>();
            emptyAsyncDisposableMock.Setup(it => it.DisposeAsync()).Returns(() => Task.CompletedTask);
            var emptyAsyncDisposable = emptyAsyncDisposableMock.Object;

            // Setup operation catalog
            var operationCatalogMock = new Mock<IWampRpcOperationCatalogProxy>();
            operationCatalogMock
                .Setup(it => it.Register(It.IsAny<IWampRpcOperation>(), It.IsAny<RegisterOptions>()))
                .Callback<IWampRpcOperation, RegisterOptions>((oper, option) => procedures.Add(oper.Procedure, oper))
                .Returns(() => Task.FromResult(emptyAsyncDisposable));
            operationCatalogMock
                .Setup(it => it.Invoke(It.IsAny<IWampRawRpcOperationClientCallback>(), It.IsAny<CallOptions>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Callback<IWampRawRpcOperationClientCallback, CallOptions, string, object[]>((caller, options, procedure, arguments) =>
                {
                    var rpcOperation = procedures[procedure];
                    rpcOperation.Invoke(rpcOperationRouterCallbackMock, new WampSharp.Newtonsoft.JsonFormatter(), null, arguments.Cast<JToken>().ToArray());
                })
                .Returns(() => (IWampCancellableInvocationProxy)null);
            var operationCatalog = operationCatalogMock.Object;

            // Mock the Realm proxy
            var realmProxyMock = new Mock<IWampRealmProxy>();
            realmProxyMock.Setup(it => it.RpcCatalog).Returns(operationCatalog);
            var realmProxy = realmProxyMock.Object;

            // Mock an IWampChannel
            var channelMock = new Mock<IWampChannel>();
            channelMock.Setup(it => it.RealmProxy).Returns(realmProxy);
            var channel = channelMock.Object;
            var channelObservable = Observable.Never<IWampChannel>().StartWith(channel);

            // Create the metadata collection
            var metadataCollection = new MetadataCollection(new[] { typeof(ITestService) }, "bla", 1, 5);

            // Create a service instance
            var service = new TestService();

            // Mock the logger
            var logger = new MockLogger<WampService>();

            // Mock the ServiceContextAccessor
            var serviceContextAccessorMock = new Mock<IServiceContextAccessor>();
            serviceContextAccessorMock.SetupProperty(it => it.ServiceContext);
            var serviceContextAccessor = serviceContextAccessorMock.Object;

            // Create a mock Dependency Injector
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(it => it.GetService(It.Is<Type>(tp => tp == typeof(IServiceContextAccessor)))).Returns(serviceContextAccessor);
            serviceProviderMock.Setup(it => it.GetService(It.Is<Type>(tp => tp == typeof(ITestService)))).Returns(service);
            var serviceProvider = serviceProviderMock.Object;

            // Mock the Service scope
            var serviceScopeMock = new Mock<IServiceScope>();
            serviceScopeMock.SetupGet(it => it.ServiceProvider).Returns(serviceProvider);
            var serviceScope = serviceScopeMock.Object;

            // Mock the Service scope factory
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            serviceScopeFactoryMock.Setup(it => it.CreateScope()).Returns(() => serviceScope);
            var serviceScopeFactory = serviceScopeFactoryMock.Object;

            // Register Service scope factory at the Dependency Injector
            serviceProviderMock.Setup(it => it.GetService(It.Is<Type>(tp => tp == typeof(IWampOperation)))).Returns(() =>
                new WampOperation(new MockLogger<WampOperation>(), serviceContextAccessor, serviceScopeFactory)
            );

            // Create an instance of WampService -> internal but visible to us using the InternalsVisibleTo-attribute in AssemblyInfo.cs.
            var wampService = new WampService(metadataCollection, logger, channelObservable);

            // Configure the WampService
            wampService.Configure(serviceProvider);
            wampService.Run();

            // Add three persons
            operationCatalog.Invoke(rpcOperationClientCallback, new CallOptions(), "bla.v1.testservice.addperson", new object[] { JToken.Parse("{ Name: 'Piet', Gender: 0, Age: 33 }") });
            operationCatalog.Invoke(rpcOperationClientCallback, new CallOptions(), "bla.v1.testservice.addperson", new object[] { JToken.Parse("{ Name: 'Jannie', Gender: 1, Age: 13 }") });
            operationCatalog.Invoke(rpcOperationClientCallback, new CallOptions(), "bla.v1.testservice.addperson", new object[] { JToken.Parse("{ Name: 'Jan', Gender: 0, Age: 35 }") });

            // Find all persons that start with "ja"
            operationCatalog.Invoke(rpcOperationClientCallback, new CallOptions(), "bla.v1.testservice.findpersons", new object[] { (JValue)"ja" });
            var values = rpcOperationClientCallback.Message as JArray;

            // Should have a result
            Assert.NotNull(values);

            // Should be an array having two records
            Assert.Equal(2, values.Count);

            // Of which each records' "Name" starts with "ja"
            Assert.All(values, v => Assert.StartsWith("ja", v["name"].Value<string>(), StringComparison.InvariantCultureIgnoreCase));

            //wampService.Dispose();
        }
    }
}