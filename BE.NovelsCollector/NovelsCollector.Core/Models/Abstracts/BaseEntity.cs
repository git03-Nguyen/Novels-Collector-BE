using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NovelsCollector.Core.Models.Abstracts
{
    public class BaseEntity : IBaseEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
    }
}
