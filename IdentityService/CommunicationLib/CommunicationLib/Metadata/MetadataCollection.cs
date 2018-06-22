using Communication.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Communication
{
    /// <summary>
    /// Contains/manages the separate metadatas for each version
    /// Metadata will be lazily created when requested for the first time.
    /// </summary>
    public class MetadataCollection
    {
        private readonly IEnumerable<Type> services;
        private readonly ConcurrentDictionary<int, Metadata> metadataCollection;
        private readonly int minVersion;
        private readonly int maxVersion;
        private readonly string prefix;

        /// <summary>
        /// Create the MetadataCollection
        /// </summary>
        /// <param name="services">A list of service-types that must be included into the metadata</param>
        /// <param name="prefix">The prefix to be used to construct the unique Fullname of the commands</param>
        /// <param name="minVersion">The minimum supported version</param>
        /// <param name="maxVersion">The maximum supported version</param>
        public MetadataCollection(IEnumerable<Type> services, string prefix, int minVersion, int maxVersion)
        {
            this.services = services;
            this.metadataCollection = new ConcurrentDictionary<int, Metadata>();
            this.minVersion = minVersion;
            this.maxVersion = maxVersion;
            this.prefix = prefix;
        }

        /// <summary>
        /// Gets the minimum supported version
        /// </summary>
        public int MinVersion { get => minVersion; }

        /// <summary>
        /// Gets the maximum supported version
        /// </summary>
        public int MaxVersion { get => maxVersion; }

        /// <summary>
        /// Returns the metadata given a certain version
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public Metadata this[int version]
        {
            get { return GetMetadata(version); }
        }

        /// <summary>
        /// Returns the metadata given a certain version
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public Metadata GetMetadata(int version)
        {
            if (version < minVersion)
                throw new VersioningException($"Version {version} not supported anymore. Minimum version: {minVersion}");
            if (version > maxVersion)
                throw new VersioningException($"Version {version} not yet supported. Maximum version: {maxVersion}");
            return metadataCollection.GetOrAdd(version, v => new MetadataBuilder(version, minVersion, maxVersion).Build(services, prefix));
        }
    }
}
