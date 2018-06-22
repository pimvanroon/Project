using PcaIdentityService.Helpers;
using System;
using Xunit;

namespace PcaIdentityService.Tests
{
    public class GeneratePasswordTest
    {
        public GeneratePasswordTest()
        {
        }

        [Fact]
        public void GeneratePassword()
        {
            var newPassword = GenerateNewPassword.GenerateRandomPassword(null);
            Assert.Contains(newPassword, ch => Char.IsDigit(ch));
            Assert.NotNull(newPassword);
        }
    }


}
