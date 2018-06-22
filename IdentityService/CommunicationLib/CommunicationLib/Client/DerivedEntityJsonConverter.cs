using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Communication.Client
{
    /// <summary>
    /// Creates a derived type instead of the (abstract) base class, when deserializing and the
    /// JSON-object contains a property "$type" with the name of the derived type.
    /// This makes lists of polymorphic objects possible.
    /// </summary>
    internal class DerivedEntityJsonConverter : JsonConverter
    {
        private readonly HashSet<Type> baseTypes;
        private readonly TypeMapper typeMapper;

        public DerivedEntityJsonConverter(TypeMapper typeMapper)
        {
            this.typeMapper = typeMapper;

            var baseTypes1 = new HashSet<Type>();
            foreach (var type1 in typeMapper.Types.Values)
            {
                Type type2 = type1.BaseType;
                while (type2 != typeof(object))
                {
                    baseTypes1.Add(type2);
                    type2 = type2.BaseType;
                }
            }
            this.baseTypes = baseTypes1;
        }

        private object Create(Type objectType, JObject obj)
        {
            string derivedTypeName = obj["$type"]?.Value<String>();
            if (!String.IsNullOrEmpty(derivedTypeName))
            {
                objectType = typeMapper[derivedTypeName];
            }
            return Activator.CreateInstance(objectType);
        }

        public override bool CanConvert(Type objectType)
        {
            return baseTypes.Contains(objectType);
        }

        public override object ReadJson(JsonReader reader,
                                        Type objectType,
                                        object existingValue,
                                        JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject jObject = JObject.Load(reader);
                object target = Create(objectType, jObject);
                serializer.Populate(jObject.CreateReader(), target);
                return target;
            }
            return null;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
