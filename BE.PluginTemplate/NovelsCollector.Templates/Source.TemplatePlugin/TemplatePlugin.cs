using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Categories;
using NovelsCollector.Domain.Resources.Chapters;
using NovelsCollector.Domain.Resources.Novels;

namespace Source.TemplatePlugin
{
    public class TemplatePlugin : ISourceFeature
    {
        public Task<Tuple<Novel[], int>> CrawlByAuthor(string authorSlug, int page = 1)
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<Novel[], int>> CrawlByCategory(string categorySlug, int page = 1)
        {
            throw new NotImplementedException();
        }

        public Task<Category[]> CrawlCategories()
        {
            throw new NotImplementedException();
        }

        public Task<Chapter?> CrawlChapter(string novelSlug, string chapterSlug)
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<Novel[], int>> CrawlCompleted(int page = 1)
        {
            throw new NotImplementedException();
        }

        public Task<Novel?> CrawlDetail(string novelSlug)
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<Novel[], int>> CrawlHot(int page = 1)
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<Novel[], int>> CrawlLatest(int page = 1)
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<Chapter[]?, int>> CrawlListChapters(string novelSlug, int page = -1)
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<Novel[]?, int>> CrawlQuickSearch(string? query, int page = 1)
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<Novel[]?, int>> CrawlSearch(string? query, int page = 1)
        {
            throw new NotImplementedException();
        }

        public Task<Chapter?> GetChapterAddrByNumber(string novelSlug, int chapterNumber)
        {
            throw new NotImplementedException();
        }
    }
}
