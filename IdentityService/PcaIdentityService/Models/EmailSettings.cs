using System;

namespace PcaIdentityService.Models
{
    public class EmailSettings
    {
        public String PrimaryDomain { get; set; }

        public int PrimaryPort { get; set; }

        public String PrimaryUsernameEmail { get; set; }

        public String PrimaryUsernamePassword { get; set; }

        public String SecondaryDomain { get; set; }

        public int SecondaryPort { get; set; }

        public String SecondaryUsernameEmail { get; set; }

        public String SecondayUsernamePassword { get; set; }
    }
}
