using System;
using System.Collections.Generic;
using System.Text;

namespace Communication.Attributes
{
    /// <summary>
    /// Indicates that the method is used to query data, i.e. not side-effecting
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class QueryAttribute: Attribute
    {
    }
}
