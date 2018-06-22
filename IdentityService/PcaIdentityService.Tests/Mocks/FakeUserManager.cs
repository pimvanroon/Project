using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PcaIdentityService.Models;

namespace PcaIdentityService.Tests
{
    public class FakeUserManager : UserManager<ApplicationUser>
    {
        public FakeUserManager(IUserStore<ApplicationUser> userStore) : base(userStore,
                  new Mock<IOptions<IdentityOptions>>().Object,
                  new Mock<IPasswordHasher<ApplicationUser>>().Object,
                  new[] { new Mock<IUserValidator<ApplicationUser>>().Object },
                  new[] { new Mock<IPasswordValidator<ApplicationUser>>().Object },
                  new Mock<ILookupNormalizer>().Object,
                  new Mock<IdentityErrorDescriber>().Object,
                  new Mock<IServiceProvider>().Object,
                  new Mock<ILogger<UserManager<ApplicationUser>>>().Object)
        { }

        public override Task<ApplicationUser> FindByEmailAsync(string email)
        {
            return Task.FromResult(new ApplicationUser { Email = email, PasswordHash = "Test12345678" });
        }

    }
}
