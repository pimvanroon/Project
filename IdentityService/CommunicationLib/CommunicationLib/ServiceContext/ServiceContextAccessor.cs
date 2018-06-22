using System.Threading;

namespace Communication
{
    internal class ServiceContextAccessor : IServiceContextAccessor
    {
        private static AsyncLocal<IServiceContext> serviceContextCurrent = new AsyncLocal<IServiceContext>();

        public IServiceContext ServiceContext
        {
            get
            {
                return serviceContextCurrent.Value;
            }
            set
            {
                serviceContextCurrent.Value = value;
            }
        }
    }
}
