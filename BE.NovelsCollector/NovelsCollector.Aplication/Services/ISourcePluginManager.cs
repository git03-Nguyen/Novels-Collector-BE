using NovelsCollector.Domain.Resources.Categories;
using NovelsCollector.Domain.Resources.Chapters;
using NovelsCollector.Domain.Resources.Novels;

namespace NovelsCollector.Aplication.Services
{
    public interface ISourcePluginManager
    {
        public Task<Tuple<Novel[]?, int>> Search(string source, string? keyword, string? title, string? author, int page = 1);
        public Task<Novel?> GetNovelDetail(string source, string novelSlug);
        public Task<Dictionary<string, Novel>?> GetNovelFromOtherSources(string excludedSource, Novel novel);
        public Task<Dictionary<string, Chapter>?> GetChapterFromOtherSources(Dictionary<string, Novel> novelInOtherSources, Chapter currentChapter);
        public Task<Chapter[]?> GetChaptersList(string source, string novelSlug, string novelId);
        public Task<Chapter?> GetChapterContent(string source, string novelSlug, string chapterSlug);
        public Task<Category[]> GetCategories(string source);
        public Task<Tuple<Novel[], int>> GetNovelsByCategory(string source, string categorySlug, int page = 1);
        public Task<Tuple<Novel[], int>> GetNovelsByAuthor(string source, string authorSlug, int page = 1);
        public Task<Tuple<Novel[], int>> GetHotNovels(string source, int page = 1);
        public Task<Tuple<Novel[], int>> GetLatestNovels(string source, int page = 1);
        public Task<Tuple<Novel[], int>> GetCompletedNovels(string source, int page = 1);
    }
}
