using System;
using System.Linq;
using System.Reflection;

namespace Communication.Versioning
{
    /// <summary>
    /// Indicates the <see cref="ServiceVersionChange"/>Change that has been done given a certain <c>Version</c>
    /// This attribute is applicable on classes, interfaces, methods, properties and Enum-fields and multiple attributes are allowed
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class ServiceVersionAttribute: Attribute
    {
        /// <summary>
        /// The version at which the change starts to have effect
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// The version's change
        /// </summary>
        public ServiceVersionChange Change { get; private set; }

        /// <summary>
        /// The old name of the item, when <c>Change</c> = <see cref="ServiceVersionChange.Renamed"/>
        /// </summary>
        public string OldName { get; set; }

        /// <summary>
        /// The old type of the item, when <c>Change</c> = <see cref="ServiceVersionChange.TypeChanged"/>
        /// </summary>
        public Type OldType { get; set; }

        public ServiceVersionAttribute(int version, ServiceVersionChange change = ServiceVersionChange.Added)
        {
            this.Version = version;
            this.Change = change;
        }

#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high --> Code is not that complex...

        /// <summary>
        /// Checks the version of the given <see cref="MemberInfo" /> (<see cref="Type"/>, <see cref="MethodInfo"/>, <see cref="PropertyInfo"/>, etc.)
        /// Returns true when the member info exists in the given version.
        /// Can possibly modify a given name when <c>Change</c> = <see cref="ServiceVersionChange.Renamed"/> and an "old" version is requested.
        /// </summary>
        /// <param name="memberInfo">The member info of which the version is checked.</param>
        /// <param name="version">The version being checked</param>
        /// <param name="name">A ref to the name the member info</param>
        /// <returns>True when the member info exists in the given version.</returns>
        public static bool CheckVersion(MemberInfo memberInfo, int version, ref string name)
        {
            bool? valid = null;
            foreach (var ver in memberInfo.GetCustomAttributes<ServiceVersionAttribute>().OrderBy(it => it.Version))
            {
                if (!valid.HasValue)
                {
                    if (ver.Change == ServiceVersionChange.Added) valid = false;
                    if (ver.Change == ServiceVersionChange.Removed) valid = true;
                }
                if (ver.Version <= version)
                {
                    if (ver.Change == ServiceVersionChange.Added) valid = true;
                    if (ver.Change == ServiceVersionChange.Removed) valid = false;
                }
                else
                {
                    if (ver.Change == ServiceVersionChange.Renamed) name = ver.OldName;
                }
            }
            return valid.GetValueOrDefault(true);
        }
    }

#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high

}
