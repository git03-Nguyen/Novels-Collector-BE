using NovelsCollector.Core.Exceptions;
using NovelsCollector.Core.Models;
using NovelsCollector.Core.Services.Abstracts;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;

namespace NovelsCollector.Core.Services
{
    public class SourcePluginsManager : BasePluginsManager<SourcePlugin, ISourcePlugin>
    {
        private const string pluginsFolderName = "source-plugins";

        public SourcePluginsManager(ILogger<SourcePluginsManager> logger, MyMongoRepository myMongoRepository)
            : base(logger, myMongoRepository, pluginsFolderName) { }


        // -------------- MANAGE FOR SOURCE FEATURES --------------
        public async Task<Tuple<Novel[]?, int>> Search(string source, string? keyword, string? title, string? author, int page = 1)
        {
            // Check if query is empty
            if (keyword == null && title == null && author == null)
                throw new BadHttpRequestException("Query is empty");

            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, search for novels
            Novel[]? novels = null;
            int totalPage = -1;
            string query = keyword ?? author ?? title ?? "";

            // Execute the plugin
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlSearch(query, page);

                // standardize the query
                var sQuery = Helpers.RemoveVietnameseSigns(query.ToLower());

                // if search by keyword, do nothing
                if (query == keyword) { }

                // filter if search by title
                else if (query == title)
                {
                    novels = novels?.Where(novel => novel.Title.ToLower().Contains(sQuery)).ToArray();
                }
                // filter if search by author
                else
                {
                    novels = novels?.Where(novel => novel.Authors[0]?.Name.ToLower().Contains(sQuery) ?? false).ToArray();
                }
            }

            if (novels == null) throw new NotFoundException("No result found");

            return new Tuple<Novel[]?, int>(novels, totalPage);
        }

        public async Task<Novel?> GetNovelDetail(string source, string novelSlug)
        {
            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the novel detail
            Novel? novel = null;

            // Execute the plugin
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                novel = await executablePlugin.CrawlDetail(novelSlug);
            }

            return novel ?? throw new NotFoundException("No result found");
        }

        public async Task<Dictionary<string, Novel>?> GetNovelFromOtherSources(string excludedSource, Novel novel)
        {
            // Check if the novel is null
            if (novel == null || novel.Title == null || novel.Authors == null || novel.Authors[0]?.Name == null)
                return null;

            // Search for the novel in other sources
            Dictionary<string, Novel> novels = new Dictionary<string, Novel>();
            foreach (var plugin in Installed)
            {
                if (plugin.Name == excludedSource) continue;

                if (plugin.PluginInstance is ISourcePlugin executablePlugin)
                {
                    // Step 1: Search by title
                    var (searchResults, _) = await executablePlugin.CrawlSearch(novel?.Title, 1);
                    if (searchResults == null) continue;

                    // Step 2: Choose the novel with the same title and author
                    var sameNovel = searchResults.FirstOrDefault(n => (n.Title == novel?.Title && n.Authors?[0]?.Name == novel?.Authors[0]?.Name));
                    if (sameNovel != null)
                        novels.Add(plugin.Name, sameNovel);
                }
            }

            if (novels.Count == 0) return null;

            // Only return the title, author and slug of each novel
            return novels.ToDictionary(kvp => kvp.Key, kvp => new Novel
            {
                Title = kvp.Value.Title,
                Slug = kvp.Value.Slug,
                Authors = [new Author { Name = kvp.Value.Authors?[0]?.Name }]
            });
        }

        public async Task<Dictionary<string, Chapter>?> GetChapterFromOtherSources(Dictionary<string, Novel> novelInOtherSources, Chapter currentChapter)
        {
            // Check if the current chapter is null
            if (currentChapter.Source == null || currentChapter.NovelSlug == null || currentChapter.Number == null ||
                novelInOtherSources.Count == 0)
            {
                return null;
            }

            // Search for the chapter in other sources
            Dictionary<string, Chapter> chapters = new Dictionary<string, Chapter>();
            string thisSource = currentChapter.Source;
            int thisChapterNumber = currentChapter.Number.Value;

            foreach (var (otherSource, otherNovel) in novelInOtherSources)
            {
                // Skip if the source is the same or the novel slug is null
                if (otherSource == thisSource || otherNovel.Slug == null) continue;

                // Get the plugin in the Installed list
                var otherPlugin = Installed.Find(p => p.Name == otherSource);
                if (otherPlugin == null) continue;

                // Execute the plugin
                if (otherPlugin.PluginInstance is ISourcePlugin executablePlugin)
                {
                    // Search for the chapter with the same number
                    var otherChapter = await executablePlugin.GetChapterAddrByNumber(otherNovel.Slug, thisChapterNumber);
                    if (otherChapter != null)
                    {
                        otherChapter.Source = otherSource;
                        chapters.Add(otherSource, otherChapter);
                    }
                }
            }

            if (chapters.Count == 0) return null;
            return chapters.ToDictionary(chapter => chapter.Key, chapter => chapter.Value);
        }

        public async Task<Tuple<Chapter[]?, int>> GetChaptersList(string source, string novelSlug, int page = -1)
        {
            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the list of chapters
            Chapter[]? chapters = null;
            int totalPage = -1;

            // Execute the plugin
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                (chapters, totalPage) = await executablePlugin.CrawlListChapters(novelSlug, page);
            }

            if (chapters == null)
            {
                throw new NotFoundException("No result found");
            }

            return new Tuple<Chapter[]?, int>(chapters, totalPage);
        }

        public async Task<Chapter?> GetChapterContent(string source, string novelSlug, string chapterSlug)
        {
            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the chapter content
            Chapter? chapter = null;

            // Execute the plugin
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                chapter = await executablePlugin.CrawlChapter(novelSlug, chapterSlug);
            }

            return chapter ?? throw new NotFoundException("No result found");
        }

        public async Task<Category[]> GetCategories(string source)
        {
            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the list of categories
            Category[]? categories = null;
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                categories = await executablePlugin.CrawlCategories();
            }

            if (categories == null)
            {
                categories = Array.Empty<Category>();
            }

            return categories;
        }

        public async Task<Tuple<Novel[], int>> GetNovelsByCategory(string source, string categorySlug, int page = 1)
        {
            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the novels by category
            Novel[]? novels = null;
            int totalPage = -1;
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlByCategory(categorySlug, page);
            }

            if (novels == null)
            {
                novels = Array.Empty<Novel>();
            }

            return new Tuple<Novel[], int>(novels, totalPage);
        }

        public async Task<Tuple<Novel[], int>> GetNovelsByAuthor(string source, string authorSlug, int page = 1)
        {
            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the novels by author
            Novel[]? novels = null;
            int totalPage = -1;
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlByAuthor(authorSlug, page);
            }

            if (novels == null)
            {
                novels = Array.Empty<Novel>();
            }

            return new Tuple<Novel[], int>(novels, totalPage);
        }

        public async Task<Tuple<Novel[], int>> GetHotNovels(string source, int page = 1)
        {
            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the hot novels
            Novel[]? novels = null;
            int totalPage = -1;
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlHot(page);
            }

            if (novels == null)
            {
                novels = Array.Empty<Novel>();
            }

            return new Tuple<Novel[], int>(novels, totalPage);
        }

        public async Task<Tuple<Novel[], int>> GetLatestNovels(string source, int page = 1)
        {
            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the latest novels
            Novel[]? novels = null;
            int totalPage = -1;
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlLatest(page);
            }

            if (novels == null)
            {
                novels = Array.Empty<Novel>();
            }

            return new Tuple<Novel[], int>(novels, totalPage);
        }

        public async Task<Tuple<Novel[], int>> GetCompletedNovels(string source, int page = 1)
        {
            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the completed novels
            Novel[]? novels = null;
            int totalPage = -1;
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlCompleted(page);
            }

            if (novels == null)
            {
                novels = Array.Empty<Novel>();
            }

            return new Tuple<Novel[], int>(novels, totalPage);
        }



    }
}
