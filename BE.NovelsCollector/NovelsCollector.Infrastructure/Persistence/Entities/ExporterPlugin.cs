using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NovelsCollector.Domain.Entities.Plugins;
using NovelsCollector.Domain.Entities.Plugins.Exporters;
using NovelsCollector.Domain.Helpers;
using System.Text.Json.Serialization;

namespace NovelsCollector.Infrastructure.Persistence.Entities
{
    public class ExporterPlugin : IExporterPlugin
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? Extension { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? Author { get; set; }
        public string? Assembly { get; set; }
        public string? Icon { get; set; }

        [BsonIgnore]
        public bool? IsLoaded { get; set; } = false;

        [BsonIgnore]
        [JsonIgnore]
        public IPluginFeature? PluginInstance { get; set; }

        [BsonIgnore]
        [JsonIgnore]
        public PluginLoadContext? LoadContext { get; set; }
        // Other specific properties for exporter plugins
    }

}
