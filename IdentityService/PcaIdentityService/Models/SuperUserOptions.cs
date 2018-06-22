using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PcaIdentityService.Models
{
    public class SuperUserOptions : User
    {
        public string Password { get; set; }
    }
}
