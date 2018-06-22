using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Communication.Client
{
    /// <summary>
    /// Convenience class to build an URL with its query parameters
    /// Contains a lot over <c>Add</c>-overloads to format primitive types correctly.
    /// </summary>
    public class TypeMapper
    {
        private readonly Dictionary<string, Type> types = new Dictionary<string, Type>();

        public TypeMapper Map<T>(string key)
        {
            return Map(key, typeof(T));
        }

        public TypeMapper Map(string key, Type value)
        {
            types[key] = value;
            return this;
        }

        public Type this[string key]
        {
            get { return types[key]; }
        }

        public IDictionary<string, Type> Types
        {
            get { return types; }
        }

    }
}
