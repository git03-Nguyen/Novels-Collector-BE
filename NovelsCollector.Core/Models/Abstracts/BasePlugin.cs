using MongoDB.Bson.Serialization.Attributes;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Plugins;
using System.Text.Json.Serialization;

namespace NovelsCollector.Core.Models.Abstracts
{
    public abstract class BasePlugin : BaseEntity
    {
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
        public IPlugin? PluginInstance { get; set; }

        [BsonIgnore]
        [JsonIgnore]
        public PluginLoadContext? LoadContext { get; set; }
    }
}
