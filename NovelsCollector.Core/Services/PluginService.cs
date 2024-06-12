using NovelsCollector.Core.Models.Abstracts;
using NovelsCollector.Core.Services.Abstracts;

namespace NovelsCollector.Core.Services
{
    public class PluginService<T> : BaseService<T>
        where T : BasePlugin, new()
    {
        public PluginService(MyMongoRepository myMongoRepository) : base(myMongoRepository)
        {
        }

        // Get all plugins 
        public async Task<IEnumerable<T>> GetAllPluginsAsync() => await GetAllAsync();

        // Add a plugin
        public async Task AddPluginAsync(T plugin) => await InsertOneAsync(plugin);

        // Delete a plugin
        public async Task DeletePluginAsync(T plugin) => await DeleteOneAsync(plugin.Id);

    }
}
