using Communication.Attributes;
using Communication.Versioning;
using PcaIdentityService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PcaIdentityService.Contracts
{
    public interface IUserService
    {
        [Query]
        [ServiceVersion(1)]
        Task<User> GetUser(string email);

        [ServiceVersion(1)]
        Task<bool> ResetPassword(string email);

        [Query]
        [ServiceVersion(2)]
        Task<IEnumerable<User>> GetUsers();

        [ServiceVersion(2)]
        Task<bool> AddUser(string email, string name = "", string primaryphone = "", string mobilephone = "", string role = "", string language = "");

        [ServiceVersion(2)]
        Task<bool> EditUser(string id, string email, string name = "", string primaryphone = "", string mobilephone = "", string role = "", string language = "", string password = "");

        [ServiceVersion(2)]
        Task<bool> RemoveUser(string id);
    }
}
