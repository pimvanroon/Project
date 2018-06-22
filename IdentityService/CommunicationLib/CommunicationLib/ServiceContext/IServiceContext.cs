using Communication.Tenant;

namespace Communication
{
    public interface IServiceContext
    {
        Metadata Metadata { get; set; }

        UserDomain UserDomain { get; set; }
    }
}