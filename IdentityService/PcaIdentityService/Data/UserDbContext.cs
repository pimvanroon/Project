using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PcaIdentityService.Models;
using Communication.Tenant;
using System;

namespace PcaIdentityService.Data
{
    public class UserDbContext : IdentityDbContext<ApplicationUser>
    {
        public class Configuration
        {
            public string ConnectionString { get; set; }
        }

        private readonly IServiceProvider provider;
        private readonly Configuration configuration;

        public UserDbContext(DbContextOptions<UserDbContext> options, IServiceProvider provider, Configuration configuration) : base(options)
        {
            this.provider = provider;
            this.configuration = configuration;
            DbInitializer.Initialize(this); 
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(configuration.ConnectionString);
        }
    }
}