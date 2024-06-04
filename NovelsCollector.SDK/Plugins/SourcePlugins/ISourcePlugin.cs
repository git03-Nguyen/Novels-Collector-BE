using NovelsCollector.SDK.Models;

namespace NovelsCollector.SDK.Plugins.SourcePlugins
{
    public interface ISourcePlugin : IPlugin
    {
        // Search for novels by keyword; TODO: author, year, ...
        public Task<Tuple<Novel[]?, int>> CrawlSearch(string? keyword, int page = 1);

        // Get novel information
        public Task<Novel?> CrawlDetail(string novelSlug);

        // Get list of chapters
        public Task<Tuple<Chapter[]?, int>> CrawlListChapters(string novelSlug, int page = -1);

        // Get chapter content
        public Task<Chapter?> CrawlChapter(string novelSlug, string chapterSlug);

        // Get list of categories
        public Task<Category[]> CrawlCategories();

        // Get novels by category
        public Task<Tuple<Novel[], int>> CrawlByCategory(string categorySlug, int page = 1);

        // ... More
    }
}
