using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Communication.Exceptions;
using Communication.Serializers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Communication.Middleware
{
    /// <summary>
    /// Middleware to provide the service's functionality using HTTP GET and POST requests
    /// A service command can be executed using a GET or POST to {baseUrl}/{service}/{command}?{query}
    /// The service version can be provided using the ServiceVersion-header.
    /// </summary>
    /// <remarks>
    /// GET-requests:
    ///    Only commands that carry the <see cref="Attributes.QueryAttribute"/> ("non side-effecting" methods) and whose
    ///    parameters are primitive types or enum-values can be executed using a GET statement. Use {query} to specify its
    ///    parameters.
    /// POST-requests:
    ///    All commands can be executed using a POST-statement. The request body can contain the following content, depending
    ///    on the Content-Type:
    ///       application/json: A JSON-object whose properties are the parameters of the command. Parameters can be primitive
    ///                         as well as complex types, like arrays of entities and enums.
    ///       application/x-www-form-urlencoded: A {query} string like if the GET-method is used.
    ///                         Only primitive types and enum-values are supported.
    /// Responses:
    ///    Both GET and POST requests return a 200 (OK) when the request is executed succesfully. Their content-body can
    ///    contain a possible returnvalue, encoded as application/json.
    /// </remarks>
    internal class ServiceMiddleware
    {
        private readonly ILogger logger;
        private readonly RequestDelegate next;

        public ServiceMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<ServiceMiddleware>();
            this.next = next;
        }

        private static Dictionary<Metadata.PrimitiveType, Func<string, object>> primitiveBinders = new Dictionary<Metadata.PrimitiveType, Func<string, object>>
        {
            { Metadata.PrimitiveType.Boolean, (s) => Boolean.Parse(s) },
            { Metadata.PrimitiveType.Byte, (s) => Byte.Parse(s, CultureInfo.InvariantCulture) },
            { Metadata.PrimitiveType.Int32, (s) => Int32.Parse(s, CultureInfo.InvariantCulture) },
            { Metadata.PrimitiveType.Int64, (s) => Int64.Parse(s, CultureInfo.InvariantCulture) },
            { Metadata.PrimitiveType.Single, (s) => Single.Parse(s, CultureInfo.InvariantCulture) },
            { Metadata.PrimitiveType.Double, (s) => Double.Parse(s, CultureInfo.InvariantCulture) },
            { Metadata.PrimitiveType.Decimal, (s) => Decimal.Parse(s, CultureInfo.InvariantCulture) },
            { Metadata.PrimitiveType.String, (s) => s },
            { Metadata.PrimitiveType.Guid, (s) => Guid.Parse(s) },
            { Metadata.PrimitiveType.DateTime, (s) => DateTime.Parse(s, CultureInfo.InvariantCulture) },
            { Metadata.PrimitiveType.DateTimeOffset, (s) => DateTimeOffset.Parse(s, CultureInfo.InvariantCulture) },
            { Metadata.PrimitiveType.TimeSpan, (s) => TimeSpan.Parse(s, CultureInfo.InvariantCulture) }
        };

        private IDictionary<string, string> SplitQuery(string query)
        {
            return query.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries).Select(it =>
            {
                int i = it.IndexOf('=');
                string key = (i == -1) ? it : it.Substring(0, i);
                string value = (i == -1) ? null : it.Substring(i + 1);
                value = WebUtility.UrlDecode(value);
                return new { key, value };
            })
           .ToDictionary(it => it.key, it => it.value);
        }

        private object ParseQueryValue(Metadata.Parameter parMeta, string value)
        {
            // Is it an enum value?
            if (parMeta.ParameterType.Type.IsEnum)
            {
                if (!Int32.TryParse(value, out int intValue))
                    throw new ServiceException(HttpStatusCode.BadRequest, $"Enum-arguments should be represented by their numeric value: {parMeta.Name}");
                return Enum.ToObject(parMeta.ParameterType.Type, intValue);
            }
            // Is it a primitive value?
            if (!Enum.TryParse(parMeta.ParameterType.Name, out Metadata.PrimitiveType primitiveType))
                throw new ServiceException(HttpStatusCode.BadRequest, $"Argument is not a primitive type: {parMeta.Name}");
            // Do we have a binder for it?
            if (!primitiveBinders.TryGetValue(primitiveType, out Func<string, object> conv))
                throw new ServiceException(HttpStatusCode.InternalServerError, $"Primitive type {primitiveType} not supported: {parMeta.Name}");
            return conv(value);
        }

        /// <summary>
        /// Returns the value of an argument, used to call a command
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> of the HTTP-request</param>
        /// <param name="parMeta">The metadata of the parameter</param>
        /// <param name="queryDict">A dictionary of query values</param>
        /// <returns></returns>
        private object GetArgumentValue(HttpContext context, Metadata.Parameter parMeta, IDictionary<string, string> queryDict)
        {
            if (parMeta.IsPlatformSpecific)
            {
                if (parMeta.ParameterType.Type == typeof(HttpContext))
                {
                    return context;
                }
                else
                {
                    throw new ServiceException(HttpStatusCode.InternalServerError, $"Unsupported platform specific parameter: {parMeta.Name}");
                }
            }
            else
            {
                bool found = queryDict.TryGetValue(parMeta.Name, out string value);
                if (!found && !parMeta.IsOptional)
                    throw new ServiceException(HttpStatusCode.BadRequest, $"Parameter missing: {parMeta.Name}");
                if (found)
                {
                    return String.IsNullOrEmpty(value) ? null : ParseQueryValue(parMeta, value);
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a list of arguments in order to call the <c>command</c>
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> of the HTTP-request</param>
        /// <param name="query">A string containing the query, separated by &amp;-signs</param>
        /// <param name="command">The command being executed</param>
        /// <returns>An array of <c>object</c> containing the command's arguments</returns>
        private object[] GetArgsFromQuery(HttpContext context, string query, Metadata.Command command)
        {
            object[] args = new object[command.Parameters.Count];
            if (query == null || query.Length < 2)
                return args;

            if (query[0] == '?')
                query = query.Substring(1);

            var queryDict = SplitQuery(query);
            for (int n = 0; n < command.Parameters.Count; n++)
            {
                args[n] = GetArgumentValue(context, command.Parameters[n], queryDict);
            }
            return args;
        }

        /// <summary>
        /// Returns a list of arguments in order to call the <c>command</c>
        /// </summary>
        /// <param name="query">A JSON-object whose properties are the parameters of the command to execute</param>
        /// <param name="command">The command being executed</param>
        /// <param name="serializer">A <see cref="PcaJsonSerializer"/> which is used to convert the JSON to values according to metadata/versioning</param>
        /// <returns>An array of <c>object</c> containing the command's arguments</returns>
        private object[] GetArgsFromJObject(HttpContext context, JObject query, Metadata.Command command, PcaJsonSerializer serializer)
        {
            Func<string, int, JToken> tokenMapper = (name, index) => query[name];
            return serializer.DeserializeArgumentList(tokenMapper, command.Parameters, null, parMeta =>
            {
                if (parMeta.ParameterType.Type == typeof(HttpContext))
                    return context;
                else
                    throw new ServiceException(HttpStatusCode.InternalServerError, $"Unsupported platform specific parameter: {parMeta.Name}");
            });
        }

        /// <summary>
        /// Execute the command, whether it is a sync or async method.
        /// Async methods are awaited.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> of the HTTP-request</param>
        /// <param name="serializer">A <see cref="PcaJsonSerializer"/> which is used to convert the JSON to values and vice versa according to metadata/versioning</param>
        /// <param name="command">The command being executed</param>
        /// <param name="args">The command's arguments</param>
        /// <returns></returns>
        private async Task ExecuteMethodAsync(HttpContext context, PcaJsonSerializer serializer, Metadata.Command command, object[] args)
        {
            var instance = context.RequestServices.GetService(command.Service.Type);
            object result = command.MethodInfo.Invoke(instance, args);

            JToken json = null;
            if (result != null)
            {
                if (result is Task task)
                {
                    await task;
                    Type resultType = result.GetType();
                    if (resultType.IsGenericType && command.ReturnType.Type != typeof(void))   // Task<T>
                    {
                        object taskResult = resultType.GetProperty("Result").GetValue(task);
                        json = serializer.Serialize(taskResult, command.ReturnType);
                    }
                }
                else
                {
                    json = serializer.Serialize(result, command.ReturnType);
                }
            }
            if (!context.Response.HasStarted && context.Response.StatusCode < 300)
            {
                if (json != null)
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync(json.ToString());
                }
                else
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("");
                }
            }
        }

        private async Task HandleGetRequest(HttpContext context, Metadata metadata, Metadata.Command command)
        {
            object[] args = GetArgsFromQuery(context, context.Request.QueryString.Value, command);
            await ExecuteMethodAsync(context, new PcaJsonSerializer(metadata), command, args);
        }

        private async Task HandlePostRequest(HttpContext context, Metadata metadata, Metadata.Command command)
        {
            object[] args;
            string content;
            var serializer = new PcaJsonSerializer(metadata);

            // Read body
            using (var ms = new MemoryStream())
            {
                await context.Request.Body.CopyToAsync(ms);
                content = Encoding.UTF8.GetString(ms.ToArray());
            }

            // Get arguments from body
            string contentType = context.Request.Headers["Content-Type"];
            if (contentType != null && contentType.Contains("application/x-www-form-urlencoded"))
            {
                args = GetArgsFromQuery(context, content, command);
            }
            else if (contentType != null && contentType.Contains("application/json"))
            {
                JObject jsonBody =
                    content == null || content.Trim() == "" ?
                    JObject.Parse("{}") :
                    JObject.Parse(content);
                args = GetArgsFromJObject(context, jsonBody, command, serializer);
            }
            else
            {
                throw new ServiceException(HttpStatusCode.UnsupportedMediaType, "Allowed Content-Types: application/x-www-form-urlencoded, application/json");
            }
            await ExecuteMethodAsync(context, serializer, command, args);
        }

        private async Task HandleRequest(HttpContext context, Metadata metadata, Metadata.Command command)
        {
            if (context.Request.Method == "GET")
            {
                if (!command.IsQuery)
                    throw new ServiceException(HttpStatusCode.BadRequest, "GET only allowed on [Query]-methods.");
                await HandleGetRequest(context, metadata, command);
            }
            else if (context.Request.Method == "POST")
            {
                await HandlePostRequest(context, metadata, command);
            }
            else
            {
                throw new ServiceException(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
            }
        }

        public async Task Invoke(HttpContext context, Metadata metadata)
        {

            if (metadata.IsEmpty) return;
            try
            {
                string[] parts = context.Request.Path.Value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || !metadata.Services.TryGetValue(parts[0], out Metadata.Service service))
                {
                    await next.Invoke(context);
                    return;
                }

                if (!service.Commands.TryGetValue(parts[1], out Metadata.Command command))
                {
                    throw new ServiceException(HttpStatusCode.NotFound, "Not Found");
                }

                if (command.ReturnType.IsObservable)
                {
                    throw new ServiceException(HttpStatusCode.NotImplemented, "Not implemented for HTTP");
                }

                await HandleRequest(context, metadata, command);
            }
            catch (ServiceException se)
            {
                context.Response.StatusCode = (int)se.HttpStatusCode;
                await context.Response.WriteAsync(se.Message);
                logger.LogError(se.InnerException ?? se, se.Message);
            }
            catch (SerializationException sze)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync(sze.Message);
                logger.LogError(sze, sze.Message);
            }
            catch (Exception e)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync("Unexpected server error");
                logger.LogError(e, e.Message);
            }
        }
    }
}
