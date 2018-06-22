using System;
using System.Collections.Generic;

namespace PcaIdentityService.Models
{
    public class User
    {
        public string Id { get; set; }

        public String UserName { get; set; }

        public String Email { get; set; }

        public String PhoneNumber { get; set; }

        public String Name { get; set; }

        public String PrimaryPhone { get; set; }

        public String MobilePhone { get; set; }

        public String Language { get; set; }

        public IEnumerable<String> RolesList { get; set;}
    }
}
