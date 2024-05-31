using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;

namespace NovelsCollector.Core.Services.Plugins.Sources
{
    public interface ISourcePluginManager : IPluginManager
    {
        public Dictionary<string, ISourcePlugin> Plugins { get; }

        Task<Tuple<Novel[]?, int>> Search(string source, string keyword, string? author, string? year, int page = 1);

        Task<Novel?> GetNovelDetail(string source, string novelSlug);

        Task<Chapter?> GetChapter(string source, string novelSlug, string chapterSlug);
    }
}
