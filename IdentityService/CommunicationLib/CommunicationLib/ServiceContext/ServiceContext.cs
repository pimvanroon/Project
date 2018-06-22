using Communication.Tenant;

namespace Communication
{
    internal class ServiceContext : IServiceContext
    {
        public Metadata Metadata { get; set; }
        public UserDomain UserDomain { get; set; }
    }
}
