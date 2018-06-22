using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PcaIdentityService.Auth;
using PcaIdentityService.Models;
using Newtonsoft.Json;

namespace PcaIdentityService.Helpers
{
    public static class Tokens
    {
        public static async Task<string> GenerateJwt(ClaimsIdentity identity, IJwtFactory jwtFactory, string userName, JsonSerializerSettings serializerSettings)
        {
            var response = new
            {
                auth_token = await jwtFactory.GenerateEncodedToken(userName, identity)
            };

            return JsonConvert.SerializeObject(response, serializerSettings);
        }
    }
}
