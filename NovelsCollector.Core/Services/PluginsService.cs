using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NovelsCollector.Core.Models.Plugins;

namespace NovelsCollector.Core.Services
{
    public class PluginsService
    {
        private readonly IMongoCollection<Source> _sourcesCollection;
        private readonly IMongoCollection<Exporter> _exportersCollection;
        private readonly ILogger _logger;

        public PluginsService(IOptions<PluginsDbSettings> pluginsDbSettings, ILogger<PluginsService> logger)
        {
            _logger = logger;
            var mongoClient = new MongoClient(pluginsDbSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(pluginsDbSettings.Value.DatabaseName);
            _sourcesCollection = mongoDatabase.GetCollection<Source>(pluginsDbSettings.Value.SourcesCollectionName);
            _exportersCollection = mongoDatabase.GetCollection<Exporter>(pluginsDbSettings.Value.ExportersCollectionName);
        }

        public async Task<IEnumerable<Source>> GetSourcesAsync()
        {
            return await _sourcesCollection.Find(source => true).ToListAsync();
        }

        public async Task<Source> GetSourceAsync(string id)
        {
            return await _sourcesCollection.Find(source => source.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Source> CreateSourceAsync(Source source)
        {
            await _sourcesCollection.InsertOneAsync(source);
            return source;
        }

        public async Task UpdateSourceAsync(string id, Source source)
        {
            await _sourcesCollection.ReplaceOneAsync(s => s.Id == id, source);
        }

        public async Task DeleteSourceAsync(string id)
        {
            await _sourcesCollection.DeleteOneAsync(source => source.Id == id);
        }

        public async Task<IEnumerable<Exporter>> GetExportersAsync()
        {
            return await _exportersCollection.Find(exporter => true).ToListAsync();
        }

        public async Task<Exporter> GetExporterAsync(string id)
        {
            return await _exportersCollection.Find(exporter => exporter.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Exporter> CreateExporterAsync(Exporter exporter)
        {
            await _exportersCollection.InsertOneAsync(exporter);
            return exporter;
        }

        public async Task UpdateExporterAsync(string id, Exporter exporter)
        {
            await _exportersCollection.ReplaceOneAsync(e => e.Id == id, exporter);
        }

        public async Task DeleteExporterAsync(string id)
        {
            await _exportersCollection.DeleteOneAsync(exporter => exporter.Id == id);
        }

    }
}
