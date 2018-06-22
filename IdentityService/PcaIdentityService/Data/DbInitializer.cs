using Microsoft.EntityFrameworkCore;

namespace PcaIdentityService.Data
{
    public static class DbInitializer
    {
        public static void Initialize(UserDbContext context)
        {
            context.Database.Migrate();
        }
    }
}
