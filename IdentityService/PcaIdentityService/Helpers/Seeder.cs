using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PcaIdentityService.Models;
using System;
using System.Threading.Tasks;

namespace PcaIdentityService.Helpers
{
    public class Seeder
    {
        public async Task EnsureSeededData(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            // Add roles
            var roleExist = await roleManager.RoleExistsAsync("admin");
            if (!roleExist)
            {
                var adminRole = new IdentityRole("admin");
                await roleManager.CreateAsync(adminRole);
            }

            // create users
            var user = await userManager.FindByEmailAsync("pcavault@gmail.com");
            if (user == null)
            {
                var adminUser = new ApplicationUser
                {
                    Email = "pcavault@gmail.com",
                    EmailConfirmed = true,
                    LockoutEnabled = false,
                    UserName = "pcavault@gmail.com",
                    Language = "en"
                };
                var createdUser = await userManager.CreateAsync(adminUser, "PcaVaultAdmin2018");
                if (createdUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "admin");
                }
            }
        }
    }
}
