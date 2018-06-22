using System;
using System.Collections.Generic;
using System.Text;

namespace Communication.Attributes
{
    /// <summary>
    /// Indicates that the property only should be serialized when
    /// its value is different from its default value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OptionalAttribute : Attribute
    {
    }
}
