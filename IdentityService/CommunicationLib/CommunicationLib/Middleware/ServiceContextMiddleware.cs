using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Communication.Exceptions;
using Communication.Tenant;
using System;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Communication.Middleware
{
    /// <summary>
    /// Builds a service context that is available everywhere using the ServiceContextAccessor
    /// </summary>
    internal class ServiceContextMiddleware
    {
        private readonly ILogger logger;
        private readonly MetadataCollection metadataCollection;
        private readonly RequestDelegate next;

        public ServiceContextMiddleware(RequestDelegate next, MetadataCollection metadataCollection, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<ServiceContextMiddleware>();
            this.metadataCollection = metadataCollection;
            this.next = next;
        }

        public async Task Invoke(HttpContext context, IServiceContextAccessor serviceContextAccessor)
        {
            try
            {
                // Collect data to go into the service context
                if (!Int32.TryParse(context?.Request?.Headers["ServiceVersion"].FirstOrDefault(), out int version))
                    version = metadataCollection.MaxVersion;
                var metadata = metadataCollection[version];

                // The userDomain is meant to identify the tenant of the user who is logging in
                UserDomain userDomain = null;
                if (context.Request.Host.Host == "localhost" || context.Request.Host.Host == "127.0.0.1")
                {
                    userDomain = new UserDomain { Name = context.Request.Query["userdomain"] };
                }
                else
                {
                    userDomain = new UserDomain { Name = context.Request.Host.Host.Replace('.', '_') } ;
                }

                // Build the service context
                var serviceContext = new ServiceContext
                {
                    Metadata = metadata,
                    UserDomain = userDomain
                };

                // Make the service context available throughout the request-handling code
                serviceContextAccessor.ServiceContext = serviceContext;

                await next.Invoke(context);
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
