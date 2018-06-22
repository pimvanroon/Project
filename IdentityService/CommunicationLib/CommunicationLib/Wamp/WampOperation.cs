using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Communication.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WampSharp.Core.Serialization;
using WampSharp.V2.Core;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Rpc;

namespace Communication.Wamp
{
    internal class WampOperation : IWampOperation
    {
        private delegate void CommandInvokerDelegate(IServiceScope serviceScope, IWampRawRpcOperationRouterCallback caller, object serviceInstance, object[] parameters, CancellationToken cancellationToken);

        private readonly ILogger logger;
        private readonly IServiceContextAccessor serviceContextAccessor;
        private readonly IServiceScopeFactory scopeFactory;

        private Metadata metadata;
        private Metadata.Command command;
        private CommandInvokerDelegate commandInvokerDelegate;
        private PcaJsonSerializer serializer;
        private bool supportsCancellation;

        public WampOperation(ILogger<WampOperation> logger, IServiceContextAccessor serviceContextAccessor, IServiceScopeFactory scopeFactory)
        {
            this.logger = logger;
            this.serviceContextAccessor = serviceContextAccessor;
            this.scopeFactory = scopeFactory;
        }

        public void Configure(Metadata metadata, Metadata.Command command)
        {
            this.serializer = new PcaJsonSerializer(metadata);
            this.metadata = metadata;
            this.command = command;
            this.commandInvokerDelegate = BuildCommandInvoker();
            this.supportsCancellation =
                // IObservable<T>
                command.MethodInfo.ReturnType.IsGenericType && typeof(IObservable<>).IsAssignableFrom(command.MethodInfo.ReturnType.GetGenericTypeDefinition()) ||
                // or one of the parameters is a CancellationToken
                command.MethodInfo.GetParameters().Any(par => par.ParameterType == typeof(CancellationToken));
        }

        public bool SupportsCancellation => supportsCancellation;

        private void HandleError(IWampRawRpcOperationRouterCallback caller, Exception e)
        {
            logger.LogError(e, $"Error executing method {command.Fullname}");
            caller.Error(WampObjectFormatter.Value, new Dictionary<string, object>() { { "error", e.Message } }, "wamp.error.runtime_error",
                    new object[] { e.Message });
        }

        // Response handlers

        private void HandleVoidSyncResult(IServiceScope serviceScope, IWampRawRpcOperationRouterCallback caller)
        {
            caller.Result(WampObjectFormatter.Value, new YieldOptions(), new object[0]);
            serviceScope?.Dispose();
        }

        private void HandleSyncResult<T>(IServiceScope serviceScope, IWampRawRpcOperationRouterCallback caller, T result)
        {
            JToken token = serializer.Serialize(result, command.ReturnType);
            caller.Result(WampObjectFormatter.Value, new YieldOptions(), new object[] { token });
            serviceScope?.Dispose();
        }

        private void HandleVoidAsyncResult(IServiceScope serviceScope, IWampRawRpcOperationRouterCallback caller, Task result)
        {
            result.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    var e = t.Exception.InnerException;
                    HandleError(caller, e);
                }
                else
                {
                    caller.Result(WampObjectFormatter.Value, new YieldOptions(), new object[0]);
                }
                serviceScope?.Dispose();
            });
        }

        private void HandleAsyncResult<T>(IServiceScope serviceScope, IWampRawRpcOperationRouterCallback caller, Task<T> result)
        {
            result.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    var e = t.Exception.InnerException;
                    HandleError(caller, e);
                }
                else
                {
                    JToken token = serializer.Serialize(t.Result, command.ReturnType);
                    caller.Result(WampObjectFormatter.Value, new YieldOptions(), new object[] { token });
                }
                serviceScope?.Dispose();
            });
        }

        private void HandleObservableResult<T>(IServiceScope serviceScope, IWampRawRpcOperationRouterCallback caller, IObservable<T> result, CancellationToken cancellationToken)
        {
            result.Subscribe(
                next =>
                {
                    JToken token = serializer.Serialize(next, command.ReturnType);
                    caller.Result(WampObjectFormatter.Value, new YieldOptions { Progress = true }, new object[] { token });
                },
                error =>
                {
                    HandleError(caller, error);
                },
                (/*completed*/) =>
                {
                    // The final result is empty and should not be used
                    JToken token = serializer.Serialize(default(T), command.ReturnType);
                    caller.Result(WampObjectFormatter.Value, new YieldOptions { Progress = false }, new object[] { token });
                    serviceScope?.Dispose();
                },
                cancellationToken
            );
        }

        // Contains method infos of the response handler above.
        // We need these to build a command invoker dynamically
        private static IDictionary<string, MethodInfo> responseHandlers =
            new Expression<Action<WampOperation>>[]
            {
                it => it.HandleVoidSyncResult(null, null),
                it => it.HandleSyncResult<object>(null, null, null),
                it => it.HandleVoidAsyncResult(null, null, null),
                it => it.HandleAsyncResult<object>(null, null, null),
                it => it.HandleObservableResult<object>(null, null, null, default(CancellationToken))
            }
            .Select(it => (it.Body as MethodCallExpression).Method)
            .ToDictionary(it => it.Name, it => it.IsGenericMethod ? it.GetGenericMethodDefinition() : it);

        // Prepares a command invoker that invokes the services' command and processes
        // the returned result, whether being a synchronous or asynchronous (Task/IObservable) type.
        private CommandInvokerDelegate BuildCommandInvoker()
        {
            // Use System.Linq.Expressions to build the invoker dynamically
            // - The parameters of the CommandInvokerDelegate
            var serviceScopePar = Expression.Parameter(typeof(IServiceScope), "serviceScope");
            var callerPar = Expression.Parameter(typeof(IWampRawRpcOperationRouterCallback), "caller");
            var servicePar = Expression.Parameter(typeof(object), "service");
            var paramsPar = Expression.Parameter(typeof(object[]), "params");
            var cancellationTokenPar = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            // Build the method call of the command
            var commandCall = Expression.Call(Expression.Convert(servicePar, command.MethodInfo.ReflectedType), command.MethodInfo,
                command.MethodInfo.GetParameters().Select((it, i) =>
                    Expression.Convert(Expression.ArrayAccess(paramsPar, Expression.Constant(i)), it.ParameterType)
                )
            );

            // Build command response handlers, depending on the return type
            Type returnType = command.MethodInfo.ReturnType;
            Expression responseHandler;
            var thisExpr = Expression.Constant(this);

            if (typeof(Task).IsAssignableFrom(returnType))
            {
                // Task / Task<T>
                if (!returnType.IsGenericType)      // Task  (void)
                    responseHandler = Expression.Call(thisExpr, responseHandlers[nameof(HandleVoidAsyncResult)],
                        serviceScopePar, callerPar, commandCall
                    );
                else   // Task<T>
                    responseHandler = Expression.Call(thisExpr, responseHandlers[nameof(HandleAsyncResult)].MakeGenericMethod(returnType.GetGenericArguments()[0]),
                        serviceScopePar, callerPar, commandCall
                    );
            }
            else if (returnType.IsGenericType && typeof(IObservable<>).IsAssignableFrom(returnType.GetGenericTypeDefinition()))
            {
                // IObservable<T>
                responseHandler = Expression.Call(thisExpr, responseHandlers[nameof(HandleObservableResult)].MakeGenericMethod(returnType.GetGenericArguments()[0]),
                    serviceScopePar, callerPar, commandCall, cancellationTokenPar
                );
            }
            else
            {
                // Synchronous result
                if (returnType == typeof(void))
                    responseHandler = Expression.Block(
                        commandCall,
                        Expression.Call(thisExpr, responseHandlers[nameof(HandleVoidSyncResult)],
                            serviceScopePar, callerPar
                        )
                    );
                else
                    responseHandler =
                        Expression.Call(thisExpr, responseHandlers[nameof(HandleSyncResult)].MakeGenericMethod(returnType),
                            serviceScopePar, callerPar, commandCall
                        );
            }
            var lambda = Expression.Lambda<CommandInvokerDelegate>(responseHandler, serviceScopePar, callerPar, servicePar, paramsPar, cancellationTokenPar);
            return lambda.Compile();
        }

        public string Procedure => command.Fullname;

        /// <summary>
        /// Setup a service context that is available everywhere, using the ServiceContextAccessor
        /// </summary>
        private void BuildServiceContext()
        {
            var serviceContext = new ServiceContext
            {
                Metadata = metadata
            };
            serviceContextAccessor.ServiceContext = serviceContext;
        }

        // Overloads of the Invoke-method
        public IWampCancellableInvocation Invoke<TMessage>(IWampRawRpcOperationRouterCallback caller, IWampFormatter<TMessage> formatter, InvocationDetails details)
            => Invoke<TMessage>(caller, formatter, details, null, null);

        public IWampCancellableInvocation Invoke<TMessage>(IWampRawRpcOperationRouterCallback caller, IWampFormatter<TMessage> formatter, InvocationDetails details, TMessage[] arguments)
            => Invoke<TMessage>(caller, formatter, details, arguments, null);

        public IWampCancellableInvocation Invoke<TMessage>(
            IWampRawRpcOperationRouterCallback caller, IWampFormatter<TMessage> formatter,
            InvocationDetails details, TMessage[] arguments,
            IDictionary<string, TMessage> argumentsKeywords)
        {
            try
            {
                // Create a separate scope for the duration of a single request
                // Scope will be disposed by the commandInvokerDelegate
                var scope = scopeFactory.CreateScope();

                CancellationTokenSource cancellationTokenSource = null;
                CancellationToken cancellationToken = default(CancellationToken);
                if (supportsCancellation)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    cancellationToken = cancellationTokenSource.Token;
                }

                // Build the service context
                BuildServiceContext();

                // Fetch the method parameters
                Func<string, int, JToken> tokenMapper = (name, index) =>
                    index < arguments.Length ? arguments[index] as JToken : null;
                object[] args = serializer.DeserializeArgumentList(tokenMapper, command.Parameters, cancellationToken, null);

                // Create a service instance
                var serviceInstance = scope.ServiceProvider.GetService(command.Service.Type);

                // Invoke the command
                commandInvokerDelegate.Invoke(scope, caller, serviceInstance, args, cancellationToken);

                if (supportsCancellation)
                    return new CancellableInvocation(cancellationTokenSource);

            }
            catch (Exception e)
            {
                HandleError(caller, e);
            }
            return null;
        }
    }
}
