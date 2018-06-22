using Microsoft.AspNetCore.Identity;
using Moq;
using Newtonsoft.Json;
using Communication.Exceptions;
using Communication.Tenant;
using PcaIdentityService.Auth;
using PcaIdentityService.Helpers;
using PcaIdentityService.Models;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Xunit;

namespace PcaIdentityService.Tests
{
    public class GenerateJwtTest
    {
        private readonly UserDomain userDomain;

        public GenerateJwtTest()
        {
            this.userDomain = new UserDomain { Name = "test123" };
        }

        [Fact]
        public async Task GenerateJwtTokenAsync()
        {
            string email = "test@test.nl";
            string password = "Test12345678";

            var identity = await GetClaimsIdentity(email, password);
            if (identity == null)
            {
                throw new ServiceException(HttpStatusCode.Unauthorized, "Failed Login attempt");
            }
            var jwtFactory = new Mock<IJwtFactory>();
            jwtFactory.Setup(It => It.GenerateEncodedToken(email, userDomain.Name, identity))
                .Returns(Task.FromResult("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJwY2F2YXVsdEBnbWFpbC5jb20iLCJqdGkiOiIwYzgzMTE3YS05NmUzLTQ4ZTYt" +
                                         "ODBjYi1jM2IwZGQ1MjYzYmIiLCJpYXQiOjE1MjQ0ODIxMjAsInJvbGUiOiJhcGlfYWNjZXNzIiwiaWQiOiI3OWZkMjU2Yi1kMDY2LTQ0MTktOG" +
                                         "I0OC1iMGZjN2M2OTkxNTQiLCJuYmYiOjE1MjQ0ODIxMjAsImV4cCI6MTUyNDQ4NTcyMCwiaXNzIjoiUGNhSWRlbnRpdHlTZXJ2aWNlIiwiYXVkI" +
                                         "joiaHR0cDovL2xvY2FsaG9zdDoyNTMxOC8ifQ.L9J4fTyN7rHyuHgPyzj6ZC179xB13HBu1ft0_4Kp_O8"));

            string token = await Tokens.GenerateJwt(identity, jwtFactory.Object, email, userDomain.Name, new JsonSerializerSettings { Formatting = Formatting.Indented });
            Assert.NotNull(token);
        }

        private async Task<ClaimsIdentity> GetClaimsIdentity(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                return await Task.FromResult<ClaimsIdentity>(null);

            var userStore = new Mock<IUserStore<ApplicationUser>>();

            var userManager = new FakeUserManager(userStore.Object);
            // get the user to verifty
            var userToVerify = await userManager.FindByEmailAsync(userName);

            if (userToVerify == null) return await Task.FromResult<ClaimsIdentity>(null);

            var jwtFactory = new Mock<IJwtFactory>();
            jwtFactory.Setup(It => It.GenerateClaimsIdentity(userName, userToVerify.Id))
                .Returns(new ClaimsIdentity(new GenericIdentity(userName, "Token"), new[]
                {
                    new Claim(Constants.Strings.JwtClaimIdentifiers.Id, userToVerify.Id),
                    new Claim(Constants.Strings.JwtClaimIdentifiers.Role, Constants.Strings.JwtClaims.ApiAccess)
                })
            );
            return await Task.FromResult(jwtFactory.Object.GenerateClaimsIdentity(userName, userToVerify.Id));
        }
    }


}
