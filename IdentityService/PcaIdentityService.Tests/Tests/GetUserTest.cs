using Microsoft.AspNetCore.Identity;
using Moq;
using PcaIdentityService.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PcaIdentityService.Tests.Tests
{
    public class GetUserTest
    {
        public GetUserTest()
        {
        }

        [Fact]
        public async Task GetUser()
        {
            var userName = "test@test.nl";
            var userStore = new Mock<IUserStore<ApplicationUser>>();

            var userManager = new FakeUserManager(userStore.Object);
            var userToVerify = await userManager.FindByEmailAsync(userName);

            Assert.Equal("test@test.nl", userToVerify.Email);
        }

        [Fact]
        public void GetUserList()
        {
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Email = "Test1@test.nl" },
                new ApplicationUser { Id = "2", Email = "Test2@test.nl" },
                new ApplicationUser { Id = "3", Email = "Test3@test.nl" },
                new ApplicationUser { Id = "4", Email = "Test4@test.nl" },
                new ApplicationUser { Id = "5", Email = "Test5@test.nl" }
            }.AsQueryable();

            var userStore = Mock.Of<IUserStore<ApplicationUser>>();
            var userManager = new Mock<FakeUserManager>(userStore);
            userManager.Setup(_ => _.Users).Returns(users);

            var userList = userManager.Object.Users;
            Assert.NotNull(userList);
        }
    }
}
