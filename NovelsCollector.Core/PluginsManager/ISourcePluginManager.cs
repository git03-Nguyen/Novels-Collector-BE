using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.SourcePlugins;

namespace NovelsCollector.Core.PluginsManager
{
    public interface ISourcePluginManager : IPluginManager
    {
        public Dictionary<string, ISourcePlugin> Plugins { get; }

        Task<Tuple<Novel[], int>> Search(string? keyword, string? author, string? year);

        Task<Novel> GetNovelDetail(string novelSlug);

        Task<string> GetChapter(string novelSlug, string chapterSlug);
    }
}
