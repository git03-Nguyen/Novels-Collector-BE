using MongoDB.Driver;
using NovelsCollector.SDK.Models.Plugins;

namespace NovelsCollector.Core.Utils
{
    public class DatabaseSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string SourcesCollectionName { get; set; }
        public string ExportersCollectionName { get; set; }
    }

    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<SourcePlugin> _sourcePlugins;
        private readonly IMongoCollection<ExporterPlugin> _exporterPlugins;

        public MongoDbContext()
        {
            var settings = new DatabaseSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "NovelsCollector",
                SourcesCollectionName = "Sources",
                ExportersCollectionName = "Exporters",
            };
            var client = new MongoClient(settings.ConnectionString);
            _database = client.GetDatabase(settings.DatabaseName);
            _sourcePlugins = _database.GetCollection<SourcePlugin>(settings.SourcesCollectionName);
            _exporterPlugins = _database.GetCollection<ExporterPlugin>(settings.ExportersCollectionName);
        }

        public IMongoCollection<SourcePlugin> SourcePlugins => _sourcePlugins;
        public IMongoCollection<ExporterPlugin> ExporterPlugins => _exporterPlugins;

    }
}
