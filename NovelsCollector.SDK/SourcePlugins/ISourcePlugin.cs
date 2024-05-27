using NovelsCollector.SDK.Models;

namespace NovelsCollector.SDK.SourcePlugins
{
    public interface ISourcePlugin : IPlugin
    {
        public string Url { get; }
        public Task<Tuple<Novel[], int>> CrawlSearch(string? keyword, int page = 1);
        public Task<Novel> CrawlDetail(string novelSlug);
        public Task<Chapter> CrawlChapter(string novelSlug, string chapterSlug);
        public Task<string> CrawChapter(Novel novel, Chapter chapter);

        // ... More
    }
}
