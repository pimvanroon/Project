using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Communication.Middleware;
using Communication.Wamp;
using System.Reactive.Linq;
using WampSharp.V2;

namespace Communication
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the MetadataCollection to the service collection
        /// Registers the Metadata to the service collection, to be requested based on the service context
        /// </summary>
        /// <param name="services"></param>
        /// <param name="metadataCollection"></param>
        /// <returns></returns>
        public static IServiceCollection AddMetadataCollection(this IServiceCollection services, MetadataCollection metadataCollection)
        {
            services.AddSingleton(provider => metadataCollection);
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.TryAddSingleton<IServiceContextAccessor, ServiceContextAccessor>();
            services.AddTransient(provider => provider.GetService<IServiceContextAccessor>().ServiceContext.Metadata);
            services.AddCors();
            services.AddScoped(provider => provider.GetService<IServiceContextAccessor>().ServiceContext.UserDomain);
            return services;
        }

        /// <summary>
        /// Installs the WAMP-communication stack
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddWamp(this IServiceCollection services)
        {
            services
                .AddSingleton(provider =>
                {
                    var settings = provider.GetService<IOptions<WampSettings>>();
                    var factory = new DefaultWampChannelFactory();
                    var channel = factory.CreateJsonChannel(settings.Value.Router, settings.Value.Realm);
                    return channel;
                })
                .AddSingleton(provider =>
                {
                    var settings = provider.GetService<IOptions<WampSettings>>();
                    var channelObservable = WampChannelFactory.CreateWampChannel(settings.Value.Router, settings.Value.Realm);
                    return channelObservable.Retry().Replay(1).RefCount();
                })
                .AddTransient<IWampOperation, WampOperation>()
                .AddSingleton<IWampService, WampService>();
            return services;
        }

        /// <summary>
        /// Configure the HTTP-communication stack
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseHttp(this IApplicationBuilder app)
        {
            app.UseCors(builder =>
            {
                builder.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin()
                    .AllowCredentials();
            });
            app.UseMiddleware<ServiceContextMiddleware>();
            app.UseMiddleware<ServiceMiddleware>();
            return app;
        }

        /// <summary>
        /// Configure the WAMP-communication stack
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseWamp(this IApplicationBuilder app)
        {
            var wampService = app.ApplicationServices.GetService<IWampService>();
            wampService.Configure(app.ApplicationServices);
            wampService.Run();
            return app;
        }
    }
}
