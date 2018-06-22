using System;
using System.Collections.Generic;
using System.Text;

namespace Communication.Attributes
{
    /// <summary>
    /// Indicates that the property not should be serialized as response
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute
    {
    }
}
