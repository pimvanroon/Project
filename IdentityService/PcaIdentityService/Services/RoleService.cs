using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using PcaIdentityService.Models;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using PcaIdentityService.Contracts;
using System.Net;
using Communication.Exceptions;

namespace PcaIdentityService.Services
{
    public class RoleService : IRoleService
    {
        private readonly IServiceProvider serviceProvider;

        public RoleService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        private UserManager<ApplicationUser> GetUserManager()
        {
            return serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        }

        private RoleManager<IdentityRole> GetRoleManager()
        {
            return serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        }

        public async Task<IEnumerable<Role>> GetRoles()
        {
            var roles = await GetRoleManager().Roles.ToListAsync();
            List<Role> rolesList = new List<Role>();
            foreach (var role in roles)
            {
                rolesList.Add(new Role
                {
                    Id = role.Id,
                    Name = role.Name
                });
            }
            return rolesList;
        }

        public async Task<bool> AddRole(string name)
        {
            if (name == null)
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "No name provided");
            }
            var roleManager = GetRoleManager();
            if (await roleManager.RoleExistsAsync(name))
            {
                return false;
            }
            else
            {
                var result = await roleManager.CreateAsync(new IdentityRole(name));
                return result.Succeeded;
            }
        }

        public async Task<bool> EditRole(string id, string name)
        {
            if (id == null)
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "No id provided");
            }
            var roleManager = GetRoleManager();
            var role = await roleManager.FindByIdAsync(id);
            role.Name = name;
            var result = await roleManager.UpdateAsync(role);
            return result.Succeeded;
        }

        public async Task<bool> RemoveRole(string id)
        {
            if (id == null)
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "No id provided");
            }
            var roleManager = GetRoleManager();
            var role = await roleManager.Roles.FirstOrDefaultAsync(x => x.Id == id);
            var userManager = GetUserManager();
            var usersWithRole = await userManager.GetUsersInRoleAsync(role.Name);
            if (usersWithRole.Count > 0)
            {
                foreach (var user in usersWithRole)
                {
                    await userManager.RemoveFromRoleAsync(user, role.Name);
                }
            }
            var result = await roleManager.DeleteAsync(role);
            return result.Succeeded;
        }

    }
}
