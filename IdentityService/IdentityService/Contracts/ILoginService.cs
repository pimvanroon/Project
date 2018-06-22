using Communication.Versioning;
using System;
using System.Threading.Tasks;

namespace PcaIdentityService.Contracts
{
    public interface ILoginService
    {
        [ServiceVersion(1)]
        Task<String> LoginAndGetJwtToken(string email, string password);
    }
}
