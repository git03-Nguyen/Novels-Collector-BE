using MongoDB.Driver;
using NovelsCollector.Aplication.Repositories;
using NovelsCollector.Domain.Entities.Plugins;

namespace NovelsCollector.Infrastructure.Persistence.Repositories
{
    public class PluginRepository<T> : IPluginRepository<T>
        where T : IPlugin, new()
    {
        private readonly MongoContext _context;
        private readonly IMongoCollection<T> _collection;

        public PluginRepository(MongoContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _collection = _context.GetCollection<T>(typeof(T).Name);
        }
        public async Task<IEnumerable<T>> GetAllPluginAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task AddPluginAsync(T plugin)
        {
            await _collection.InsertOneAsync(plugin);
        }

        public async Task RemovePluginAsync(T plugin)
        {
            await _collection.DeleteOneAsync(x => x.Id == plugin.Id);
        }
    }
}
