using NovelsCollector.SDK.Models;

namespace NovelsCollector.SDK.Plugins.SourcePlugins
{
    public interface ISourcePlugin : IPlugin
    {
        // Search for novels by query: keyword, author, ...
        public Task<Tuple<Novel[]?, int>> CrawlSearch(string? query, int page = 1);

        // Get novel information
        public Task<Novel?> CrawlDetail(string novelSlug);

        // Get list of chapters
        public Task<Tuple<Chapter[]?, int>> CrawlListChapters(string novelSlug, int page = -1);

        // Get chapter content
        public Task<Chapter?> CrawlChapter(string novelSlug, string chapterSlug);
        public Task<Chapter?> GetChapterSlug(string novelSlug, int chapterNumber);

        // Get list of categories
        public Task<Category[]> CrawlCategories();

        // Get novels by category
        public Task<Tuple<Novel[], int>> CrawlByCategory(string categorySlug, int page = 1);

        // Get novels by author
        public Task<Tuple<Novel[], int>> CrawlByAuthor(string authorSlug, int page = 1);

        // Get hot novels
        public Task<Tuple<Novel[], int>> CrawlHot(int page = 1);

        // Get latest novels
        public Task<Tuple<Novel[], int>> CrawlLatest(int page = 1);

        // Get completed novels
        public Task<Tuple<Novel[], int>> CrawlCompleted(int page = 1);


        // ... More
    }
}
