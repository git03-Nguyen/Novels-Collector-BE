using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using NovelsCollector.Domain.Entities;

namespace NovelsCollector.Infrastructure.Persistence
{
    public class MongoContext
    {
        private readonly IMongoDatabase _database;

        public MongoContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetSection("DatabaseSettings:ConnectionString").Value;
            var databaseName = configuration.GetSection("DatabaseSettings:DatabaseName").Value;

            var mongoClient = new MongoClient(connectionString);
            _database = mongoClient.GetDatabase(databaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string name) where T : IEntity, new()
        {
            return _database.GetCollection<T>(name);
        }

    }
}
