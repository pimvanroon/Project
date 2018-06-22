using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using PcaIdentityService.Auth;
using PcaIdentityService.Contracts;
using PcaIdentityService.Helpers;
using PcaIdentityService.Models;
using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Communication.Tenant;
using Microsoft.Extensions.DependencyInjection;
using Communication.Exceptions;

namespace PcaIdentityService.Services
{
    public class LoginService: ILoginService
    {
        private readonly IJwtFactory jwtFactory;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly Seeder seeder;
        private readonly IServiceProvider serviceProvider;

        public LoginService(IJwtFactory jwtFactory, UserManager<ApplicationUser> userManager,
            Seeder seeder, IServiceProvider serviceProvider)
        {
            this.jwtFactory = jwtFactory;
            this.userManager = userManager;
            this.seeder = seeder;
            this.serviceProvider = serviceProvider;
        }

        private SignInManager<ApplicationUser> GetSignInManager()
        {
            return serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        }

        public async Task<String> LoginAndGetJwtToken(string email, string password)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "No email provided");
            }
            else if (string.IsNullOrEmpty(password))
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "No password provided");
            }
            else
            {
                await seeder.EnsureSeededData(serviceProvider);
                var identity = await GetClaimsIdentity(email, password);
                if (identity == null)
                {
                    throw new ServiceException(HttpStatusCode.Unauthorized, "Failed Login attempt");
                }
                var signInManager = GetSignInManager();
                await signInManager.PasswordSignInAsync(email, password, false, lockoutOnFailure: true);
                return await Tokens.GenerateJwt(identity, jwtFactory, email, new JsonSerializerSettings { Formatting = Formatting.Indented });
            }
        }

        private async Task<ClaimsIdentity> GetClaimsIdentity(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                return await Task.FromResult<ClaimsIdentity>(null);

            // get the user to verifty
            var userToVerify = await userManager.FindByNameAsync(userName);

            if (userToVerify == null) return await Task.FromResult<ClaimsIdentity>(null);

            // check the credentials
            if (await userManager.CheckPasswordAsync(userToVerify, password))
            {
                return await Task.FromResult(jwtFactory.GenerateClaimsIdentity(userName, userToVerify.Id));
            }

            // Credentials are invalid, or account doesn't exist
            return await Task.FromResult<ClaimsIdentity>(null);
        }
    }
}
