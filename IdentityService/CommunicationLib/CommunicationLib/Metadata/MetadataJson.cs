using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Communication
{
    /// <summary>
    /// JSON specific
    /// </summary>
    public partial class Metadata
    {
        [JsonProperty("enums")]
        private IList<Enum> JsonEnums { get; set; }

        [JsonProperty("entities")]
        private IList<Entity> JsonEntities { get; set; }

        [JsonProperty("services")]
        private IList<Service> JsonServices { get; set; }

        public partial class Service
        {
            [JsonProperty("commands")]
            internal IList<Command> JsonCommands { get; set; }
        }

        public partial class Enum
        {
            [JsonProperty("values")]
            internal IList<EnumValue> JsonValues { get; set; }
        }

        public partial class Entity
        {
            [JsonProperty("properties")]
            internal IList<Property> JsonProperties { get; set; }
        }

        public partial class Command
        {
            [JsonProperty("parameters")]
            internal IList<Parameter> JsonParameters { get; set; }
        }

        /// <summary>
        /// Returns the metadata in JSON format
        /// </summary>
        /// <returns></returns>
        public string AsJson()
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };

            // Convert dictionaries to lists
            JsonEnums = Enums?.Values.ToList();
            JsonEntities = Entities?.Values.ToList();
            JsonServices = Services?.Values.ToList();
            foreach (var serviceMeta in JsonServices)
            {
                serviceMeta.JsonCommands = serviceMeta.Commands.Values.ToList();
                foreach (var commandMeta in serviceMeta.JsonCommands)
                {
                    commandMeta.JsonParameters = commandMeta.Parameters.Where(it => !it.IsPlatformSpecific).ToList();
                }
            }
            foreach (var enumMeta in JsonEnums)
            {
                enumMeta.JsonValues = enumMeta.Values.Values.ToList();
            }
            foreach (var entityMeta in JsonEntities)
            {
                entityMeta.JsonProperties = entityMeta.Properties.Values.ToList();
            }
            return JsonConvert.SerializeObject(this, settings);
        }

        /// <summary>
        /// Constructs metadata from JSON format
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Metadata FromJson(string json)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };
            var metadata = JsonConvert.DeserializeObject<Metadata>(json, settings);

            // Convert lists to dictionaries
            metadata.Enums = metadata.JsonEnums.ToDictionary(it => it.Name, StringComparer.InvariantCultureIgnoreCase);
            metadata.Entities = metadata.JsonEntities.ToDictionary(it => it.Name, StringComparer.InvariantCultureIgnoreCase);
            metadata.Services = metadata.JsonServices.ToDictionary(it => it.Name, StringComparer.InvariantCultureIgnoreCase);
            foreach (var serviceMeta in metadata.JsonServices)
            {
                serviceMeta.Commands = serviceMeta.JsonCommands.ToDictionary(it => it.Name, StringComparer.InvariantCultureIgnoreCase);
                foreach (var command in serviceMeta.JsonCommands)
                {
                    command.Parameters = command.JsonParameters.ToList();
                }
            }
            foreach (var enumMeta in metadata.JsonEnums)
            {
                enumMeta.Values = enumMeta.JsonValues.ToDictionary(it => it.Name, StringComparer.InvariantCultureIgnoreCase);
            }
            foreach (var entityMeta in metadata.JsonEntities)
            {
                entityMeta.Properties = entityMeta.JsonProperties.ToDictionary(it => it.Name, StringComparer.InvariantCultureIgnoreCase);
            }
            return metadata;
        }
    }
}
