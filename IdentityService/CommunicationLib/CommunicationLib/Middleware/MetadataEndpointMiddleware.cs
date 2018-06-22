using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Communication.Exceptions;
using System;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Communication.Middleware
{
    /// <summary>
    /// Middleware to provide the metadata of a requested service version as a JSON-document
    /// The metadata can be requested by a GET on the url {baseUrl}/metadata.
    /// The service version can be provided using the ServiceVersion-header.
    /// </summary>
    public class MetadataEndpointMiddleware
    {
        private readonly ILogger logger;
        private readonly RequestDelegate next;

        public MetadataEndpointMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<MetadataEndpointMiddleware>();
            this.next = next;
        }

        public async Task Invoke(HttpContext context, MetadataCollection metadataCollection)
        {
            try
            {
                if (context.Request.Method == "GET" && context.Request.Path.Value.Equals("/metadata", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!Int32.TryParse(context?.Request?.Headers["ServiceVersion"].FirstOrDefault(), out int version))
                        version = metadataCollection.MaxVersion;
                    var metadata = metadataCollection[version];

                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(metadata.AsJson());
                }
                else
                {
                    await next.Invoke(context);
                }
            }
            catch (ServiceException se)
            {
                logger.LogError(se.InnerException, se.Message);
                context.Response.StatusCode = (int)se.HttpStatusCode;
                await context.Response.WriteAsync(se.Message);
            }
            catch (SerializationException sze)
            {
                logger.LogError(sze, sze.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync(sze.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync("Unexpected server error");
            }
        }
    }
}
