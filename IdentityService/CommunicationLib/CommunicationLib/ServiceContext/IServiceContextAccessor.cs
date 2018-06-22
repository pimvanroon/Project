using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Communication
{
    public interface IServiceContextAccessor
    {
        IServiceContext ServiceContext { get; set; }
    }
}
