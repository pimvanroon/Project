using Communication.Attributes;
using Communication.Exceptions;
using Communication.Versioning;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Communication
{
    /// <summary>
    /// Builds the metadata for a given set of service types
    /// </summary>
    public class MetadataBuilder
    {
        /// <summary>
        /// This is the list of supported primitive types
        /// </summary>
        private static Dictionary<Type, Metadata.PrimitiveType> typeMap = new Dictionary<Type, Metadata.PrimitiveType>
        {
            { typeof(Boolean),         Metadata.PrimitiveType.Boolean },
            { typeof(Byte),            Metadata.PrimitiveType.Byte },
            { typeof(Int32),           Metadata.PrimitiveType.Int32 },
            { typeof(Int64),           Metadata.PrimitiveType.Int64 },
            { typeof(Single),          Metadata.PrimitiveType.Single },
            { typeof(Double),          Metadata.PrimitiveType.Double },
            { typeof(Decimal),         Metadata.PrimitiveType.Decimal },
            { typeof(String),          Metadata.PrimitiveType.String },
            { typeof(Guid),            Metadata.PrimitiveType.Guid },
            { typeof(DateTime),        Metadata.PrimitiveType.DateTime },
            { typeof(DateTimeOffset),  Metadata.PrimitiveType.DateTimeOffset },
            { typeof(TimeSpan),        Metadata.PrimitiveType.TimeSpan }
        };

        private readonly int version;
        private readonly int minVersion;
        private readonly int maxVersion;
        private readonly Dictionary<Type, Metadata.Entity> entities = new Dictionary<Type, Metadata.Entity>();
        private readonly Dictionary<Type, Metadata.Enum> enums = new Dictionary<Type, Metadata.Enum>();

        /// <summary>
        /// Constructor to create a MetadataBuilder for the given version
        /// <c>minVersion</c> and <c>maxVersion</c> are used for reference only.
        /// </summary>
        /// <param name="version">The version for which to generate the metadata</param>
        /// <param name="minVersion">The minimum supported version</param>
        /// <param name="maxVersion">The maximum supoorted version</param>
        public MetadataBuilder(int version, int minVersion, int maxVersion)
        {
            this.version = version;
            this.minVersion = minVersion;
            this.maxVersion = maxVersion;
        }

        private Dictionary<string, Metadata.Property> CreateEntityProperties(Type entityType, HashSet<Assembly> assemblies)
        {
            var propMetas = new Dictionary<string, Metadata.Property>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var prop in entityType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)

                // Virtual properties are skipped, they are used for lazy loading which is not supported for now
                // Ignore-properties are skipped, they are meant for internal/database usage
                .Where(it => !it.GetGetMethod().IsVirtual && !it.IsDefined(typeof(IgnoreAttribute)))
            )
            {
                string propName = ExternalNameAttribute.GetName(prop);
                if (ServiceVersionAttribute.CheckVersion(prop, version, ref propName))
                {
                    var propMeta = new Metadata.Property
                    {
                        Name = propName.ToCamelCase(),
                        PropertyType = ConvertType(prop.PropertyType, assemblies),
                        PropertyInfo = prop,
                        IsRequired = prop.GetCustomAttribute<RequiredAttribute>() != null,
                        IsOptional = prop.GetCustomAttribute<OptionalAttribute>() != null,
                        MaxLength = prop.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength
                    };
                    propMetas.Add(propMeta.Name, propMeta);
                }
            }
            return propMetas;
        }

        private Metadata.Entity CreateEntityMeta(Type entityType, HashSet<Assembly> assemblies)
        {
            string entityName = ExternalNameAttribute.GetName(entityType);
            if (ServiceVersionAttribute.CheckVersion(entityType, version, ref entityName))
            {
                Metadata.Entity parentEntity = null;
                if (entityType.BaseType != null && entityType.BaseType.IsClass && entityType.BaseType != typeof(Object))
                {
                    parentEntity = GetOrAddEntityMeta(entityType.BaseType, assemblies);
                    if (entities.TryGetValue(entityType, out Metadata.Entity existingEntity))
                        return existingEntity;
                }
                var entity = new Metadata.Entity
                {
                    Name = entityName,
                    Properties = CreateEntityProperties(entityType, assemblies),
                    Parent = parentEntity,
                    ParentEntity = parentEntity?.Name,
                    Type = entityType
                };
                entities.Add(entityType, entity);

                var derivedTypes = assemblies.SelectMany(it => it.GetTypes().Where(tp => entityType.IsAssignableFrom(tp) && !tp.IsAbstract && tp.IsPublic));
                foreach (var derivedType in derivedTypes)
                {
                    GetOrAddEntityMeta(derivedType, assemblies);
                }

                return entity;
            }
            else
            {
                throw new MetadataBuilderException($"Entity {entityName} not available to version {version}, but still refered to");
            }
        }

        private Metadata.Entity GetOrAddEntityMeta(Type entityType, HashSet<Assembly> assemblies)
        {
            if (entityType == typeof(void))
            {
                return new Metadata.Entity
                {
                    Name = "Void",
                    Type = entityType
                };
            }
            // Limit the entity types to the assemblies where the services are defined
            if (!assemblies.Contains(entityType.Assembly))
            {
                throw new MetadataBuilderException($"Type {entityType.Name} is not defined in the same assembly as the service(s)");
            }
            if (!entities.TryGetValue(entityType, out Metadata.Entity entity))
            {
                entity = CreateEntityMeta(entityType, assemblies);
            }
            return entity;
        }

        private Metadata.Enum GetOrAddEnumMeta(Type enumType)
        {
            if (!enums.TryGetValue(enumType, out Metadata.Enum metaEnum))
            {
                string enumName = ExternalNameAttribute.GetName(enumType);
                if (ServiceVersionAttribute.CheckVersion(enumType, version, ref enumName))
                {
                    var enumValues = new Dictionary<string, Metadata.EnumValue>(StringComparer.InvariantCultureIgnoreCase);
                    foreach (var field in enumType.GetFields())
                    {
                        if (field.IsLiteral)
                        {
                            int value = (int)field.GetValue(null);
                            string fieldName = ExternalNameAttribute.GetName(field);
                            if (ServiceVersionAttribute.CheckVersion(field, version, ref fieldName))
                            {
                                var enumValue = new Metadata.EnumValue
                                {
                                    Name = fieldName,
                                    Value = value,
                                    FieldInfo = field
                                };
                                enumValues.Add(enumValue.Name, enumValue);
                            }
                        }
                    }
                    metaEnum = new Metadata.Enum
                    {
                        Name = enumName,
                        Values = enumValues,
                        Type = enumType
                    };
                    enums.Add(enumType, metaEnum);
                }
            }
            return metaEnum;
        }

        private Metadata.TypeInfo ConvertType(Type type, HashSet<Assembly> assemblies)
        {
            bool isObservable = false;
            if (type.IsGenericType && typeof(IObservable<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                isObservable = true;
                type = type.GetGenericArguments()[0];
            }

            if (type.IsGenericType && typeof(Task<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                type = type.GetGenericArguments()[0];
            }
            else if (typeof(Task).IsAssignableFrom(type))
            {
                type = typeof(void);
            }

            bool isArray = false;
            if (type.IsArray)
            {
                isArray = true;
                type = type.GetElementType();
            }
            if (type.IsGenericType && (
                    type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                    type.GetInterfaces().Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                )
            )
            {
                isArray = true;
                type = type.GetGenericArguments()[0];
            }

            bool isPrimitive = !isArray && !isObservable;

            Type baseType = type;
            bool isNullable = false;
            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
            {
                isNullable = true;
                baseType = nullableType;
            }

            string typeName = null;
            bool isCancellationToken = false;
            if (baseType == typeof(CancellationToken))
            {
                typeName = baseType.Name;
                isCancellationToken = true;
            }
            else if (typeMap.TryGetValue(baseType, out Metadata.PrimitiveType value))
            {
                typeName = value.ToString();
            }
            else if (baseType.IsEnum)
            {
                typeName = GetOrAddEnumMeta(baseType).Name;
            }
            else
            {
                typeName = GetOrAddEntityMeta(baseType, assemblies).Name;
                isPrimitive = false;
            }

            return new Metadata.TypeInfo
            {
                IsArray = isArray,
                IsNullable = isNullable,
                IsObservable = isObservable,
                IsPrimitive = isPrimitive,
                IsCancellationToken = isCancellationToken,
                Name = typeName,

                Type = type
            };
        }

        private Metadata.Command BuildCommand(string prefix, Metadata.Service service, MethodInfo command, HashSet<Assembly> assemblies)
        {
            string commandName = ExternalNameAttribute.GetName(command);
            if (ServiceVersionAttribute.CheckVersion(command, version, ref commandName))
            {
                var parMetas = new List<Metadata.Parameter>();
                bool isHttpRaw = false;
                foreach (var par in command.GetParameters())
                {
                    if (par.ParameterType == typeof(Microsoft.AspNetCore.Http.HttpContext))
                    {
                        isHttpRaw = true;
                        var parMeta = new Metadata.Parameter
                        {
                            IsOptional = par.IsOptional,
                            IsPlatformSpecific = true,
                            ParameterType = new Metadata.TypeInfo { Type = par.ParameterType },
                            ParameterInfo = par
                        };
                        parMetas.Add(parMeta);
                    }
                    else
                    {
                        string parName = ExternalNameAttribute.GetName(par);
                        var parMeta = new Metadata.Parameter
                        {
                            Name = parName,
                            IsOptional = par.IsOptional,
                            ParameterType = ConvertType(par.ParameterType, assemblies),
                            ParameterInfo = par
                        };
                        parMetas.Add(parMeta);
                    }
                }
                var commandMeta = new Metadata.Command
                {
                    Name = commandName.ToCamelCase(),
                    Fullname = prefix + commandName.ToLower(),
                    Parameters = parMetas,
                    ReturnType = ConvertType(command.ReturnType, assemblies),
                    MethodInfo = command,
                    Service = service,
                    IsQuery = command.IsDefined(typeof(QueryAttribute)),
                    IsHttpRaw = isHttpRaw
                };
                return commandMeta;
            }
            return null;
        }

        private Metadata.Service BuildService(Type service, string prefix, HashSet<Assembly> assemblies)
        {
            string serviceName = ExternalNameAttribute.GetName(service);
            if (ServiceVersionAttribute.CheckVersion(service, version, ref serviceName))
            {
                var commandMetas = new Dictionary<string, Metadata.Command>(StringComparer.InvariantCultureIgnoreCase);
                var serviceMeta = new Metadata.Service { Name = serviceName, Type = service };
                foreach (var command in service.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    var commandMeta = BuildCommand($"{prefix}.v{version}.{serviceName}.".ToLower(), serviceMeta, command, assemblies);
                    if (commandMeta != null) commandMetas.Add(commandMeta.Name, commandMeta);
                }
                serviceMeta.Commands = commandMetas;
                return serviceMeta;
            }
            return null;
        }

        /// <summary>
        /// Builds the metadata for the given set of <c>services</c>.
        /// </summary>
        /// <param name="services">The <see cref="Type" /> of the services</param>
        /// <param name="prefix">The prefix to be used to construct the unique Fullname of the commands</param>
        /// <returns>The built metadata</returns>
        /// <remarks>
        /// The <see cref="Type" /> of a service can be either an interface or a concrete service class.
        /// It's this type that has to contain <see cref="ExternalNameAttribute"/> and <see cref="ServiceVersionAttribute"/> the attributes.
        /// </remarks>
        public Metadata Build(IEnumerable<Type> services, string prefix)
        {
            var serviceMetas = new Dictionary<string, Metadata.Service>(StringComparer.InvariantCultureIgnoreCase);

            var assemblies = new HashSet<Assembly>(services.Select(it => it.Assembly).Distinct());
            foreach (var service in services)
            {
                var serviceMeta = BuildService(service, prefix, assemblies);
                if (serviceMeta != null)
                {
                    serviceMetas.Add(serviceMeta.Name, serviceMeta);
                }
            }
            return new Metadata
            {
                Enums = enums.Values.ToDictionary(it => it.Name, StringComparer.InvariantCultureIgnoreCase),
                Entities = entities.Values.ToDictionary(it => it.Name, StringComparer.InvariantCultureIgnoreCase),
                Services = serviceMetas,
                MinVersion = minVersion,
                MaxVersion = maxVersion,
                CurVersion = version
            };
        }
    }
}
