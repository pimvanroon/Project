using System;

namespace PcaIdentityService.Models
{
    public class IdentitySettings
    {
        public Boolean RequireConfirmedEmail { get; set; }

        public Boolean RequireConfirmedPhoneNumber { get; set; }

        public Boolean RequireDigit { get; set; }

        public int RequiredLength { get; set; }

        public Boolean RequireNonAlphanumeric { get; set; }

        public Boolean RequireUppercase { get; set; }

        public Boolean RequireLowercase { get; set; }

        public int RequiredUniqueChars { get; set; }

        public int DefaultLockoutTimeSpanMinutes { get; set; }

        public int MaxFailedAccessAttempts { get; set; }

        public Boolean AllowedForNewUsers { get; set; }

        public Boolean RequireUniqueEmail { get; set; }

    }
}
