using NovelsCollector.Domain.Entities.Plugins;

namespace NovelsCollector.Application.Repositories
{
    public interface IPluginRepository<T> where T : IPlugin
    {
        public Task<IEnumerable<T>> GetAllPluginAsync();

        public Task AddPluginAsync(T plugin);

        public Task RemovePluginAsync(T plugin);
    }
}
