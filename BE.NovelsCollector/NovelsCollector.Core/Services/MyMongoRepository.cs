using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NovelsCollector.Core.Models;

namespace NovelsCollector.Core.Services
{
    public class MyMongoRepository
    {
        public IMongoDatabase mongoDatabase;

        public MyMongoRepository(IOptions<DatabaseSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            mongoDatabase = client.GetDatabase(settings.Value.DatabaseName);
        }
    }
}
