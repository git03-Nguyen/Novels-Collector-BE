using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NovelsCollector.Core.Models.Plugins
{
    public class Source
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("dll")]
        public string Dll { get; set; }

        [BsonElement("url")]
        public string Url { get; set; }
    }
}
