using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;

namespace NovelsCollector.Core.PluginsManager
{
    public interface ISourcePluginManager : IPluginManager
    {
        public Dictionary<string, ISourcePlugin> Plugins { get; }

        Task<Novel[]> Search(string? keyword, string? author, string? year);

        Task<Novel> GetNovelDetail(string novelSlug);

        Task<string> GetChapter(string novelSlug, string chapterSlug);
    }
}
