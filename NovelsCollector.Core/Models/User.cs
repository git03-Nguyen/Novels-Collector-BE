using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NovelsCollector.Core.Models.Abstracts;

namespace NovelsCollector.Core.Models
{
    public class User : BaseEntity
    {

        [BsonElement("Email")]
        public string Email { get; set; }

        [BsonElement("Password")]
        public string Password { get; set; }

        [BsonElement("Role")]
        public string Role { get; set; } = "Admin"; // Default role is Admin
    }
}
