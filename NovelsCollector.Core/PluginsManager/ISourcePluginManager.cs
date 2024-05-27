using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;

namespace NovelsCollector.Core.PluginsManager
{
    public interface ISourcePluginManager : IPluginManager
    {
        public Dictionary<string, ISourcePlugin> Plugins { get; }

        Task<Novel[]> Search(string? keyword, string? author, string? year);

        Task<Novel> GetNovelDetail(Novel novel);

        Task<string> GetChapter(Novel novel, Chapter chapter);
    }
}
