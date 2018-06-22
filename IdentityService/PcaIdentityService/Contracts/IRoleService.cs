using Communication.Attributes;
using Communication.Versioning;
using PcaIdentityService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PcaIdentityService.Contracts
{
    public interface IRoleService
    {
        [Query]
        [ServiceVersion(2)]
        Task<IEnumerable<Role>> GetRoles();

        [ServiceVersion(2)]
        Task<bool> AddRole(string name);

        [ServiceVersion(2)]
        Task<bool> EditRole(string id, string name);

        [ServiceVersion(2)]
        Task<bool> RemoveRole(string id);
    }
}
