using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NovelsCollector.Core.Models.Plugins
{
    public class Exporter
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("extension")]
        public string Extension { get; set; }

        [BsonElement("dll")]
        public string Dll { get; set; }
    }
}
