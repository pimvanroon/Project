using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Communication.Versioning
{
    /// <summary>
    /// Change the name of the given item. This determines how the name of the item appears in the generated metadata
    /// (and externally to requests/responses)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Interface, AllowMultiple = true)]
    public class ExternalNameAttribute: Attribute
    {
        public string Name { get; private set; }

        public ExternalNameAttribute(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Get the native name of the given MemberInfo (Type, MethodInfo, PropertyInfo, etc.)
        /// If the MemberInfo has an ExternalName attribute, the name is overriden by that of the attribute.
        /// The native name is modified in the following circumstances:
        ///   - When MemberInfo is an interface: The leading I is removed (so "IService" becomes "Service")
        ///   - When MemberInfo is a method info: The trailing Async is removed (so "GetDataAsync" becomes "GetData")
        /// </summary>
        /// <param name="member">The member info of which the name is obtained</param>
        /// <returns>The member's (modified) name, possibly overriden by ExternalName-attribute</returns>
        public static string GetName(MemberInfo member)
        {
            var attr = member.GetCustomAttribute<ExternalNameAttribute>();
            if (attr != null)
                return attr.Name;

            // Omit the first 'I' of an interface name
            if (member is Type type && type.IsInterface && type.Name.Length > 2 && type.Name.StartsWith('I') && Char.IsUpper(type.Name[1]))
                return type.Name.Substring(1);

            // When a method name ends with "Async" (because it's implemented as async-method returning a Task)
            // strip the "Async"-part
            if (member is MethodInfo methodInfo)
            {
                string methodName = methodInfo.Name;
                return methodName.EndsWith("Async") && typeof(Task).IsAssignableFrom(methodInfo.ReturnType) ?
                    methodName.Substring(0, methodName.Length - "Async".Length) :
                    methodName;
            }

            return member.Name;
        }

        /// <summary>
        /// Get the native name of the given ParameterInfo (separate overload, ParameterInfo does not descend from MemberInfo)
        /// If the MemberInfo has an ExternalName attribute, the name is overriden by that of the attribute.
        /// </summary>
        /// <param name="parameterInfo">The parameter info of which the name is obtained</param>
        /// <returns>The parameter info's name, possibly overriden by ExternalName-attribute</returns>
        public static string GetName(ParameterInfo parameterInfo)
        {
            var attr = parameterInfo.GetCustomAttribute<ExternalNameAttribute>();
            return attr != null ? attr.Name : parameterInfo.Name;
        }
    }
}
