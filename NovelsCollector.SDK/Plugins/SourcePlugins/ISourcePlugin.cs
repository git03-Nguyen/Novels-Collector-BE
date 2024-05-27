using NovelsCollector.SDK.Models;

namespace NovelsCollector.SDK.Plugins.SourcePlugins
{
    public interface ISourcePlugin : IPlugin
    {
        public string Url { get; }
        public Task<Novel[]> CrawlSearch(string? keyword);
        public Task<Novel> CrawlDetail(Novel novel);
        public Task<string> CrawlChapter(Novel novel, Chapter chapter);

        // ... More
    }
}
