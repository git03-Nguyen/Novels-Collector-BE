using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NovelsCollector.SDK.Models.Plugins
{
    public abstract class BasePlugin
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? Author { get; set; }
        public bool Enabled { get; set; }
    }
}
