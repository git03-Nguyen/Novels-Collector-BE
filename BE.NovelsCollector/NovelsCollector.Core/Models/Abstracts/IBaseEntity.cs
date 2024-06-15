using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NovelsCollector.Core.Models.Abstracts
{
    // This interface is used to mark all entities saved in the database
    public interface IBaseEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
    }
}
