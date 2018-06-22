using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PcaIdentityService.Contracts;
using PcaIdentityService.Helpers;
using PcaIdentityService.Models;
using PcaIdentityService.Internal_Services;
using Communication.Exceptions;
using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace PcaIdentityService.Services
{
    public class UserService : IUserService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IdentitySettings identitysettings;
        private readonly IEmailSender emailSender;

        public UserService(IServiceProvider serviceProvider, IOptions<IdentitySettings> identitysettings, IEmailSender emailSender)
        {
            this.serviceProvider = serviceProvider;
            this.identitysettings = identitysettings.Value;
            this.emailSender = emailSender;
        }

        private UserManager<ApplicationUser> GetUserManager()
        {
            return serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        }

        public async Task<IEnumerable<User>> GetUsers()
        {
            var users = await GetUserManager().Users.ToListAsync();
            List<User> usersList = new List<User>();
            foreach (var usr in users)
            {
                usersList.Add(new User
                {
                    Id = usr.Id,
                    UserName = usr.UserName,
                    Email = usr.Email,
                    PhoneNumber = usr.PhoneNumber,
                    Name = usr.Name,
                    PrimaryPhone = usr.PrimaryPhone,
                    MobilePhone = usr.MobilePhone,
                    Language = usr.Language,
                    RolesList = await GetUserManager().GetRolesAsync(usr)
                });
            }
            return usersList;
        }

        public async Task<User> GetUser(string email)
        {
            if (String.IsNullOrEmpty(email))
            {
                return null;
            }
            else
            {
                var userManager = GetUserManager();
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return null;
                }
                else
                {
                    User peterConnectsUser = (new User()
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Name = user.Name,
                        PrimaryPhone = user.PrimaryPhone,
                        MobilePhone = user.MobilePhone,
                        Language = user.Language,
                        RolesList = await GetUserManager().GetRolesAsync(user)
                    });
                    return peterConnectsUser;
                }
            }
        }

        public async Task<bool> ResetPassword(string email)
        {
            if (String.IsNullOrEmpty(email))
            {
                throw new ServiceException(HttpStatusCode.Unauthorized, "No email address provided");
            }
            var userManager = GetUserManager();
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                throw new ServiceException(HttpStatusCode.Unauthorized, "No user found");
            }
            else
            {
                var password = GenerateNewPassword.GenerateRandomPassword(identitysettings);
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, token, password);
                if (result.Succeeded)
                {
                    // send email to the user with the new password
                    await emailSender.SendEmailAsync(email, "Confirmation reset password",
                        $"Your password has been reset to: " + password + "<br><br> Do not forget to change your password on your profile page.");
                    return true;
                }
                else
                {
                    throw new ServiceException(HttpStatusCode.Conflict, "Errors" + result.Errors);
                }
            }
        }

        public async Task<bool> AddUser(string email, string name = "", string primaryphone = "", string mobilephone = "", string role = "", string language = "")
        {
            if (String.IsNullOrEmpty(email))
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "No email address provided");
            }
            ApplicationUser peterConnectsUser = (new ApplicationUser()
            {
                UserName = email,
                Name = name,
                Email = email,
                PrimaryPhone = primaryphone,
                MobilePhone = mobilephone,
                Language = language,
                EmailConfirmed = true,
            });
            var userManager = GetUserManager();
            var password = GenerateNewPassword.GenerateRandomPassword(identitysettings);
            var createdUser = await userManager.CreateAsync(peterConnectsUser, password);
            if (createdUser.Succeeded)
            {
                // add role to user if its not empty
                if (!String.IsNullOrEmpty(role))
                {
                    await userManager.AddToRoleAsync(peterConnectsUser, role);
                }
                else
                {
                    await userManager.AddToRoleAsync(peterConnectsUser, "member");
                }
                // send email to the newly created user
                await emailSender.SendEmailAsync(email, "Welcome to PeterConnects",
                    $"your user account has been created<br><br>Login with " + email + "<br><br> Password is: " + password);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> EditUser(string id, string email, string name = "", string primaryphone = "", string mobilephone = "", string role = "", string language = "", string password = "")
        {
            if (String.IsNullOrEmpty(email))
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "No email address provided");
            }
            var userManager = GetUserManager();
            var user = await userManager.FindByIdAsync(id);
            user.Email = email;
            user.Name = name;
            user.PrimaryPhone = primaryphone;
            user.MobilePhone = mobilephone;
            user.Language = language;
            if (!String.IsNullOrEmpty(password))
            {
                if (await userManager.HasPasswordAsync(user))
                {
                    await userManager.RemovePasswordAsync(user);
                }
                await userManager.AddPasswordAsync(user, password);
            }
            if (!String.IsNullOrEmpty(role))
            {
                var userRole = await userManager.GetRolesAsync(user);
                if (userRole.Count > 0)
                    await userManager.RemoveFromRoleAsync(user, userRole[0]);
                await userManager.AddToRoleAsync(user, role);
            }
            var result = await userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> RemoveUser(string id)
        {
            if (id == null)
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "No id provided");
            }
            var userManager = GetUserManager();
            var user = await userManager.FindByIdAsync(id);
            if (user != null)
            {
                var rolesForUser = await userManager.GetRolesAsync(user);
                if (rolesForUser.Count > 0)
                {
                    foreach (var role in rolesForUser.ToList())
                    {
                        await userManager.RemoveFromRoleAsync(user, role);
                    }
                }
            }
            var result = await userManager.DeleteAsync(user);
            return result.Succeeded;
        }

    }
}
