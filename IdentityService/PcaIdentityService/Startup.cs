using Communication;
using Communication.Middleware;
using Communication.Wamp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PcaIdentityService.Auth;
using PcaIdentityService.Contracts;
using PcaIdentityService.Data;
using PcaIdentityService.Helpers;
using PcaIdentityService.Internal_Services;
using PcaIdentityService.Models;
using PcaIdentityService.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace PcaIdentityService
{
    internal static class Methods
    {
        public static IServiceCollection ConfigureOtherOptionDependentOption<TOptions>(this IServiceCollection services, Action<TOptions, IServiceProvider> action) where TOptions : class, new()
        {
            services.AddSingleton<TOptions>(provider =>
            {
                var options = new TOptions();
                action(options, provider);
                return options;
            });
            return services;
        }
    }

    public class Startup
    {
        private readonly IConfigurationRoot configuration;
        private readonly MetadataCollection metadataCollection;
        private const int MinVersion = 1;
        private const int MaxVersion = 2;

        private const string SecretKey = "iNivDABLpUA223oelfhqGbMRdRj1PVkH"; // todo: get this from somewhere secure
        private readonly SymmetricSecurityKey signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey));

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile($"appsettings.{Environment.UserName}.json", optional: true)
                .AddEnvironmentVariables();
            configuration = builder.Build();

            metadataCollection = new MetadataCollection(
                new[] { typeof(IUserService), typeof(ILoginService), typeof(IRoleService) },
                "com.peterconnects",
                MinVersion, MaxVersion
            );
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .Configure<JwtAppSettingOptions>(configuration.GetSection("JwtIssuerOptions"))
                .Configure<IdentitySettings>(configuration.GetSection("IdentitySettings"))
                .Configure<EmailSettings>(configuration.GetSection("EmailSettings"))
                .Configure<SuperUserOptions>(configuration.GetSection("SuperUserSettings"))
                .Configure<WampSettings>(configuration.GetSection("Wamp"));

            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddSingleton<IJwtFactory, JwtFactory>();
            services.AddSingleton<Seeder>();

            IdentitySettings identitySettings;
            JwtAppSettingOptions jwtOptions;
            JwtAppSettingOptions jwtAppSettingOptions;
            using (var optionsServiceProvider = services.BuildServiceProvider())
            {
                identitySettings = optionsServiceProvider.GetService<IOptions<IdentitySettings>>().Value;
                jwtOptions = optionsServiceProvider.GetService<IOptions<JwtAppSettingOptions>>().Value;
                jwtAppSettingOptions = optionsServiceProvider.GetService<IOptions<JwtAppSettingOptions>>().Value;
            }

            services.AddMetadataCollection(metadataCollection)
                    .AddWamp()
                    .AddTransient<IUserService, UserService>()
                    .AddTransient<ILoginService, LoginService>()
                    .AddTransient<IRoleService, RoleService>();

            services.AddDbContext<UserDbContext>(
                ServiceLifetime.Transient, ServiceLifetime.Singleton
            );
            services.AddSingleton(it => new UserDbContext.Configuration { ConnectionString = configuration.GetConnectionString("DefaultConnection") });

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<UserDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedEmail = identitySettings.RequireConfirmedEmail;
                options.SignIn.RequireConfirmedPhoneNumber = identitySettings.RequireConfirmedPhoneNumber;

                options.Password.RequireDigit = identitySettings.RequireDigit;
                options.Password.RequiredLength = identitySettings.RequiredLength;
                options.Password.RequireNonAlphanumeric = identitySettings.RequireNonAlphanumeric;
                options.Password.RequireUppercase = identitySettings.RequireUppercase;
                options.Password.RequireLowercase = identitySettings.RequireLowercase;
                options.Password.RequiredUniqueChars = identitySettings.RequiredUniqueChars;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identitySettings.DefaultLockoutTimeSpanMinutes);
                options.Lockout.MaxFailedAccessAttempts = identitySettings.MaxFailedAccessAttempts;
                options.Lockout.AllowedForNewUsers = identitySettings.AllowedForNewUsers;

                options.User.RequireUniqueEmail = identitySettings.RequireUniqueEmail;
            });

            // Configure JwtIssuerOptions
            services.Configure<JwtIssuerOptions>(options =>
            {
                options.Issuer = jwtOptions.Issuer;
                options.Audience = jwtOptions.Audience;
                options.SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
                options.ValidFor = TimeSpan.FromHours(jwtOptions.ValidForHours);
            });

            // Token validation parameters
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(configureOptions =>
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtAppSettingOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtAppSettingOptions.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,

                    RequireExpirationTime = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                configureOptions.ClaimsIssuer = jwtAppSettingOptions.Issuer;
                configureOptions.TokenValidationParameters = tokenValidationParameters;
                configureOptions.SaveToken = true;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            IConfigurationSection configSectionLogging = configuration.GetSection("Logging");
            loggerFactory.AddConsole(configSectionLogging)
                         .AddDebug()
                         .AddFile(configSectionLogging.GetValue<string>("LogFile"));

            app.UseMiddleware<MetadataEndpointMiddleware>();
            app.UseWamp();
            app.UseHttp();

            Seeder seeder = new Seeder();
            seeder.EnsureSeededData(serviceProvider).Wait();
        }
    }
}
