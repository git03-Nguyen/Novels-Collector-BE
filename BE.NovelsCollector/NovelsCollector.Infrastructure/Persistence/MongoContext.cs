using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NovelsCollector.Domain.Entities;
using NovelsCollector.Infrastructure.Persistence.Configurations;

namespace NovelsCollector.Infrastructure.Persistence
{
    public class MongoContext
    {
        private readonly IMongoDatabase _database;
        private readonly Settings _settings;

        public MongoContext(IOptions<Settings> options)
        {
            _settings = options.Value;
            var connectionString = _settings.ConnectionString;
            var databaseName = _settings.DatabaseName;

            var mongoClient = new MongoClient(connectionString);
            _database = mongoClient.GetDatabase(databaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string name) where T : IEntity, new()
        {
            return _database.GetCollection<T>(name);
        }

    }
}
