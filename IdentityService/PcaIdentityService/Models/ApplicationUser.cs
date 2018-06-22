using Microsoft.AspNetCore.Identity;
using System;

namespace PcaIdentityService.Models
{
    public class ApplicationUser : IdentityUser
    {
        public String Name { get; set; }

        public String PrimaryPhone { get; set; }

        public String MobilePhone { get; set; }

        public String Language { get; set; }
    }
}
