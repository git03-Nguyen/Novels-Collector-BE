using NovelsCollector.Domain.Entities.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelsCollector.Aplication.Repositories
{
    public interface IPluginRepository<T> where T : IPlugin, new()
    {
        public Task<IEnumerable<T>> GetAllPluginAsync();

        public Task AddPluginAsync(T plugin);

        public Task RemovePluginAsync(T plugin);
    }
}
