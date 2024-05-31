using NovelsCollector.SDK.Models;

namespace NovelsCollector.SDK.Plugins.SourcePlugins
{
    public interface ISourcePlugin : IPlugin
    {
        public string Url { get; }
        public Task<Tuple<Novel[], int>> CrawlSearch(string? keyword, int page = 1);
        public Task<Novel> CrawlDetail(Novel novel);
        public Task<string> CrawlChapter(Novel novel, Chapter chapter);

        // ... More
    }
}
