using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Communication
{
    /// <summary>
    /// The metadata that is used to describe one or more <i>services</i> containing one or more
    /// <i>commands</i> of a given <c>version</c>. The metadata can be seen as a <i>contract</i>
    /// that describes the interface of a service exactly.
    /// The metadata is used internally to translate communication requests to calls of methods and
    /// to translate the result to a communication response (using the <see cref="JsonSerializer" />).
    /// The metadata can also me made public (as a JSON-file) using the <see cref="Middleware.MetadataEndpointMiddleware"/>
    /// </summary>
    public partial class Metadata
    {
        /// <summary>
        /// The primitive types supported by the Metadata
        /// </summary>
        [Serializable]
        public enum PrimitiveType
        {
            Void,

            Boolean,
            Byte,
            Int32,
            Int64,
            Single,
            Double,
            Decimal,
            String,
            Guid,
            DateTime,
            DateTimeOffset,
            TimeSpan
        }

        /// <summary>
        /// Describes the type of a property, parameter or return value
        /// </summary>
        public class TypeInfo
        {
            /// <summary>
            /// The basic type name
            /// </summary>
            [JsonProperty("name")]
            public string Name { get; set; }

            /// <summary>
            /// Maximum length for string types
            /// </summary>
            [JsonProperty("maxLength")]
            [DefaultValue(0)]
            public int MaxLength { get; set; }

            /// <summary>
            /// True when <c>TypeInfo</c> is an array
            /// </summary>
            [JsonProperty("isArray")]
            [DefaultValue(false)]
            public bool IsArray { get; set; }

            /// <summary>
            /// True when <c>TypeInfo</c> is a nullable type
            /// </summary>
            [JsonProperty("isNullable")]
            [DefaultValue(false)]
            public bool IsNullable { get; set; }

            /// <summary>
            /// True when <c>TypeInfo</c> is an observable.
            /// Communication layer should implement this as a request having a response that 'streams' multiple results
            /// </summary>
            [JsonProperty("isObservable")]
            [DefaultValue(false)]
            public bool IsObservable { get; set; }

            /// <summary>
            /// True when the <c>TypeInfo</c> is a primitive type or an Enum
            /// </summary>
            [JsonIgnore]
            internal bool IsPrimitive { get; set; }

            /// <summary>
            /// True when the <c>TypeInfo</c> is a CancellationToken
            /// </summary>
            [JsonIgnore]
            internal bool IsCancellationToken { get; set; }

            /// <summary>
            /// The underlying type of the item
            /// </summary>
            [JsonIgnore]
            internal System.Type Type { get; set; }

            /// <summary>
            /// Returns the type info of an array element when this type info is an array
            /// </summary>
            /// <returns>The array's element info</returns>
            public TypeInfo GetElementInfo()
            {
                return new TypeInfo
                {
                    Name = this.Name,
                    MaxLength = this.MaxLength,
                    IsArray = false,
                    IsNullable = this.IsNullable,
                    IsObservable = this.IsObservable,
                    IsPrimitive = this.IsPrimitive,
                    Type = this.Type,
                };
            }

            public override string ToString()
            {
                string name = Name;
                if (IsNullable) name += "?";
                if (IsArray) name += "[]";
                return name;
            }
        }

        /// <summary>
        /// Represents one of the values of an enum type
        /// </summary>
        public class EnumValue
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("value")]
            public int Value { get; set; }

            [JsonIgnore]
            internal FieldInfo FieldInfo { get; set; }

            public override string ToString()
            {
                return $"{Name} = {Value}";
            }
        }

        /// <summary>
        /// Represents an enum type
        /// </summary>
        public partial class Enum
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonIgnore]
            public IDictionary<string, EnumValue> Values { get; set; }

            [JsonIgnore]
            internal System.Type Type { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// Represents a property
        /// </summary>
        public class Property
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("type")]
            public TypeInfo PropertyType { get; set; }

            [JsonProperty("isRequired")]
            [DefaultValue(false)]
            public bool IsRequired { get; set; }

            [JsonProperty("maxLength")]
            [DefaultValue(null)]
            public int? MaxLength { get; set; }

            [JsonProperty("isKey")]
            [DefaultValue(false)]
            public bool IsKey { get; set; }

            [JsonProperty("isOptional")]
            [DefaultValue(false)]
            public bool IsOptional { get; set; }

            [JsonIgnore]
            internal PropertyInfo PropertyInfo { get; set; }

            public override string ToString()
            {
                return $"{PropertyType} {Name}";
            }

        }

        /// <summary>
        /// Represents an entity.
        /// </summary>
        public partial class Entity
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonIgnore]
            public IDictionary<string, Property> Properties { get; set; }

            [JsonIgnore]
            internal System.Type Type { get; set; }

            [JsonIgnore]
            public Entity Parent { get; set; }

            [JsonProperty("parent")]
            public string ParentEntity { get; set; }

            public override string ToString()
            {
                return $"{Name}";
            }
        }

        /// <summary>
        /// Represents a command's parameter
        /// </summary>
        public class Parameter
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("type")]
            public TypeInfo ParameterType { get; set; }

            [JsonProperty("isOptional")]
            [DefaultValue(false)]
            public bool IsOptional { get; set; }

            /// <summary>
            /// Indicates that this argument is communication platform specific, hidden from the metadata
            /// </summary>
            [JsonIgnore]
            internal bool IsPlatformSpecific { get; set; }

            [JsonIgnore]
            internal ParameterInfo ParameterInfo { get; set; }

            public override string ToString()
            {
                return $"{ParameterType} {Name}";
            }
        }

        /// <summary>
        /// Represents a command
        /// </summary>
        public partial class Command
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("fullname")]
            public string Fullname { get; set; }

            [JsonIgnore]
            public IList<Parameter> Parameters { get; set; }

            [JsonProperty("returnType")]
            public TypeInfo ReturnType { get; set; }

            [JsonProperty("isQuery")]
            public bool IsQuery { get; set; }

            /// <summary>
            /// Indicates that this method works directly on a HTTP-context
            /// using Request & Response Body, headers, etc.
            /// </summary>
            [JsonProperty("isHttpRaw")]
            public bool IsHttpRaw { get; internal set; }

            [JsonIgnore]
            public Service Service { get; set; }

            [JsonIgnore]
            internal MethodInfo MethodInfo { get; set; }

            public override string ToString()
            {
                return $"{Name}";
            }
        }

        /// <summary>
        /// Represnets a Service
        /// </summary>
        public partial class Service
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonIgnore]
            public IDictionary<string, Command> Commands { get; set; }

            [JsonIgnore]
            public System.Type Type { get; set; }

            public override string ToString()
            {
                return $"{Name}";
            }
        }

        /// <summary>
        /// A dictionary of all supported Enums
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, Enum> Enums { get; set; }

        /// <summary>
        /// A dictionary of all supported Entities and Complex Types
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, Entity> Entities { get; set; }

        /// <summary>
        /// A dictionary of all services
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, Service> Services { get; set; }

        /// <summary>
        /// The current version of this metadata
        /// </summary>
		[JsonProperty("curVersion")]
        public int CurVersion { get; set; }

        /// <summary>
        /// The minimum version available on the service
        /// </summary>
		[JsonProperty("minVersion")]
        public int MinVersion { get; set; }

        /// <summary>
        /// The maximum version available on the service
        /// </summary>
		[JsonProperty("maxVersion")]
        public int MaxVersion { get; set; }


        public static Metadata Empty
        {
            get { return new Metadata(); }
        }

        public bool IsEmpty
        {
            get
            {
                return
                    (Enums == null || !Enums.Any()) &&
                    (Entities == null || !Entities.Any()) &&
                    (Services == null || !Services.Any());
            }
        }
    }
}
