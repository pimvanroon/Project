using Newtonsoft.Json.Linq;
using Communication.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Communication.Serializers
{
    /// <summary>
    /// Serializes and Deserializes to/from JSON-objects (Newtonsoft.Json.Linq.JToken)
    /// with respect to the metadata/versioning
    /// </summary>
    public class PcaJsonSerializer
    {
        private readonly Metadata metadata;

        public PcaJsonSerializer(Metadata metadata)
        {
            this.metadata = metadata;
        }

        private object DeserializeArray(JToken jsonValue, Metadata.TypeInfo typeInfo)
        {
            var jsonArr = jsonValue as JArray;
            if (jsonArr == null)
                throw new SerializationException("Array type expected");
            var array = Array.CreateInstance(typeInfo.Type, jsonArr.Count);
            for (int n = 0; n < jsonArr.Count; n++)
            {
                array.SetValue(Deserialize(jsonArr[n], typeInfo.GetElementInfo()), n);
            }
            return array;
        }

        private object DeserializeEnum(JToken jsonValue, Metadata.Enum enumMeta)
        {
            int intValue = (int)jsonValue;
            var enumValue = enumMeta.Values.Values.FirstOrDefault(it => it.Value == intValue);
            if (enumValue == null)
            {
                string possibleValues = String.Join(", ", enumMeta.Values.Values.Select(it => $"{it.Name} = {it.Value}"));
                throw new SerializationException($"Unknown enum-value {intValue} on Enum {enumMeta.Name}. Possible values are: {possibleValues}");
            }
            return Enum.ToObject(enumMeta.Type, intValue);
        }

        private object DeserializeObject(JObject jsonObj, Metadata.Entity entity)
        {
            if (jsonObj == null)
                throw new SerializationException("Object type expected");
            object instance = Activator.CreateInstance(entity.Type);
            var handled = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var prop in jsonObj.Properties())
            {
                if (entity.Properties.TryGetValue(prop.Name, out Metadata.Property propMeta))
                {
                    object value = Deserialize(prop.Value, propMeta.PropertyType);
                    propMeta.PropertyInfo.SetValue(instance, value);
                    handled.Add(prop.Name);
                }
            }
            // Check IsRequired
            var missing = entity.Properties.Values.Where(it => it.IsRequired && !handled.Contains(it.Name)).ToList();
            if (missing.Any())
            {
                string props = (missing.Count == 1 ? "property " : "properties ") +
                    String.Join(", ", missing.Select(it => it.Name));
                throw new SerializationException($"Missing required {props} on {entity.Name}");
            }
            return instance;
        }

        /// <summary>
        /// Deserializes a list of JTokens to a list of arguments in order to call a method
        /// </summary>
        /// <param name="tokenMapper">A function that returns a JToken given its name</param>
        /// <param name="parameterList">A list of parameters</param>
        /// <returns>A list of arguments. It always has the same order and count as <c>parameterList</c></returns>
        public object[] DeserializeArgumentList(Func<string, int, JToken> tokenMapper, IList<Metadata.Parameter> parameterList, CancellationToken? cancellationToken, Func<Metadata.Parameter, object> platformSpecificMapper)
        {
            object[] args = new object[parameterList.Count];
            for (int n = 0; n < parameterList.Count; n++)
            {
                var parMeta = parameterList[n];
                if (parMeta.IsPlatformSpecific)
                {
                    args[n] = platformSpecificMapper?.Invoke(parMeta);
                }
                else
                if (parMeta.ParameterType.IsCancellationToken)
                {
                    args[n] = cancellationToken;
                }
                else
                {
                    JToken token = tokenMapper(parMeta.Name, n);
                    if (token == null && !parMeta.IsOptional)
                        throw new SerializationException($"Parameter missing: {parMeta.Name}");
                    if (token != null)
                    {
                        args[n] = Deserialize(token, parMeta.ParameterType);
                    }
                }
            }
            return args;
        }

        /// <summary>
        /// Deserializes a <c>jsonValue</c>
        /// </summary>
        /// <param name="jsonValue"></param>
        /// <param name="typeInfo"></param>
        /// <returns></returns>
        public object Deserialize(JToken jsonValue, Metadata.TypeInfo typeInfo)
        {
            if (jsonValue is JValue val && val.Value == null)
                return null;

            // Arrays
            if (typeInfo.IsArray)
            {
                return DeserializeArray(jsonValue, typeInfo);
            }

            // Enums
            if (metadata.Enums.TryGetValue(typeInfo.Name, out Metadata.Enum enumMeta))
            {
                return DeserializeEnum(jsonValue, enumMeta);
            }

            // Primitive types
            if (Enum.TryParse(typeInfo.Name, out Metadata.PrimitiveType result))
            {
                return jsonValue.ToObject(typeInfo.Type);
            }

            // Entity
            if (metadata.Entities.TryGetValue(typeInfo.Name, out Metadata.Entity entity))
            {
                return DeserializeObject(jsonValue as JObject, entity);
            }

            throw new SerializationException("Unknown type: " + typeInfo.Name);
        }

        private JToken SerializeArray(object value, Metadata.TypeInfo typeInfo)
        {
            var array = value as IEnumerable;
            var tokens = new List<JToken>();
            foreach (object arrValue in array)
            {
                JToken token = Serialize(arrValue, typeInfo.GetElementInfo());
                tokens.Add(token);
            }
            return new JArray(tokens.ToArray());
        }

        private JToken SerializeEnum(object value)
        {
            return new JValue((int)value);
        }

        private bool IsDefaultValue(object value, Type objectType)
        {
            if (!objectType.IsValueType)
                return value == null;
            object defaultValue = Activator.CreateInstance(objectType);
            return defaultValue == null ?
                value == null :
                defaultValue.Equals(value);
        }

        private JToken SerializeEntity(object value, Metadata.Entity entity, Metadata.TypeInfo typeInfo)
        {
            var jObject = new JObject();

            // Different type: this should be a derived type
            Type realType = value.GetType();

            var allProps = entity.Properties;
            if (realType != typeInfo.Type)
            {
                allProps = new Dictionary<string, Metadata.Property>();
                if (!typeInfo.Type.IsAssignableFrom(realType))
                    throw new SerializationException($"Type mismatch: {realType.Name} is not a {typeInfo.Type}");
                entity = metadata.Entities.Values.FirstOrDefault(it => it.Type == realType);
                if (entity == null)
                    throw new SerializationException($"Type {realType.Name} is not supported in this version ({metadata.CurVersion})");

                jObject["$type"] = entity.Name;

                // Get all properties of base and derived types
                do
                {
                    foreach (var pair in entity.Properties)
                    {
                        allProps[pair.Key] = pair.Value;
                    }
                    entity = entity.Parent;
                }
                while (entity != null);
            }
            foreach (var propMeta in allProps.Values)
            {
                object propValue = propMeta.PropertyInfo.GetValue(value, null);
                if (!propMeta.IsOptional || !IsDefaultValue(propValue, propMeta.PropertyInfo.PropertyType))
                {
                    JToken token = Serialize(propValue, propMeta.PropertyType);
                    jObject[propMeta.Name] = token;
                }
            }
            return jObject;
        }

        public JToken Serialize(object value, Metadata.TypeInfo typeInfo)
        {
            if (value == null)
                return JValue.CreateNull();

            // Arrays
            if (typeInfo.IsArray)
            {
                return SerializeArray(value, typeInfo);
            }

            // Enums
            if (metadata.Enums.TryGetValue(typeInfo.Name, out Metadata.Enum enumMeta))
            {
                return SerializeEnum(value);
            }

            // Primitive types
            if (Enum.TryParse(typeInfo.Name, out Metadata.PrimitiveType result))
            {
                return result == Metadata.PrimitiveType.Void ? JValue.CreateNull() : new JValue(value);
            }

            // Entity
            if (metadata.Entities.TryGetValue(typeInfo.Name, out Metadata.Entity entity))
            {
                return SerializeEntity(value, entity, typeInfo);
            }

            throw new SerializationException("Unknown type: " + typeInfo.Name);
        }
    }
}
