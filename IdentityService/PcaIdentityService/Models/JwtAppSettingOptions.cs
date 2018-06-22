using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PcaIdentityService.Models
{
    public class JwtAppSettingOptions
    {
        public String Issuer { get; set; }

        public String Audience { get; set; }

        public int ValidForHours { get; set; }
    }
}
