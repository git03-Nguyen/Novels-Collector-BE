using Microsoft.Extensions.Caching.Memory;
using NovelsCollector.Core.Exceptions;
using NovelsCollector.Core.Models;
using NovelsCollector.Core.Services.Abstracts;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.Collections.Concurrent;

namespace NovelsCollector.Core.Services
{
    public class SourcePluginsManager : BasePluginsManager<SourcePlugin, ISourcePlugin>
    {
        private const string pluginsFolderName = "Sources";

        private IMemoryCache _cacheService;


        public SourcePluginsManager(ILogger<SourcePluginsManager> logger, MyMongoRepository myMongoRepository, IMemoryCache cacheServide)
            : base(logger, myMongoRepository, pluginsFolderName)
        {
            _cacheService = cacheServide;
        }


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
                // standardize the query
                var sQuery = Helpers.RemoveVietnameseSigns(query.ToLower());
                (novels, totalPage) = await executablePlugin.CrawlSearch(sQuery, page);

                // if search by keyword, do nothing
                if (query == keyword) { }

                // filter if search by title
                else if (query == title)
                {
                    novels = novels?.Where(novel => Helpers.RemoveVietnameseSigns(novel.Title).ToLower().Contains(sQuery)).ToArray();
                }
                // filter if search by author
                else
                {
                    novels = novels?.Where(novel => Helpers.RemoveVietnameseSigns(novel.Authors[0]?.Name).ToLower().Contains(sQuery)).ToArray();
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


            // Caching the same novels in all sources
            var cacheKey = $"novels-{novel.Title}-{novel.Authors[0]?.Name}";
            if (_cacheService.TryGetValue(cacheKey, out Dictionary<string, Novel> cachedNovels))
            {
                _logger.LogInformation($"Cache hit for novels of {novel.Title} by {novel.Authors[0]?.Name}");
                // copy the cachedNovels to a new dictionary, without the excluded source
                var cachedResult = cachedNovels.Where(kvp => kvp.Key != excludedSource).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return cachedResult.Count == 0 ? null : cachedResult;
            }

            // Search for the novel in other sources
            var novels = new ConcurrentDictionary<string, Novel>();

            // Using threads parallel to search for the novel in other sources, each thread for each plugin
            var tasks = Installed.Select(plugin => Task.Run(() => SearchForNovelInPlugin(plugin, excludedSource, novel, novels))).ToArray();

            // Wait for all tasks to finish
            await Task.WhenAll(tasks);

            // Ensure to stop every tasks
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                {
                    _logger.LogError(task.Exception?.InnerException, "Error when searching for novels in other sources");
                }
            }

            while (!novels.TryAdd(excludedSource, novel)) ;

            // If no novel is found, return null
            if (novels.Count == 0) return null;

            // Set null the no-needed properties
            var result = novels.ToDictionary(kvp => kvp.Key, kvp => new Novel
            {
                Title = kvp.Value.Title,
                Slug = kvp.Value.Slug,
                Authors = [new Author { Name = kvp.Value.Authors?[0]?.Name }]
            });

            // Cache the same novels in all sources
            _cacheService.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                SlidingExpiration = TimeSpan.FromMinutes(30),
                Size = 1
            });

            // Return new dictionary without the excluded source
            return result.Where(kvp => kvp.Key != excludedSource).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        // Method to handle the search logic for a single plugin
        private async Task SearchForNovelInPlugin(SourcePlugin plugin, string excludedSource, Novel novel, ConcurrentDictionary<string, Novel> novels)
        {
            if (plugin.Name == excludedSource) return;

            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                try
                {
                    // Step 1: Search by title
                    var (searchResults, _) = await executablePlugin.CrawlQuickSearch(novel.Title, 1);
                    if (searchResults == null) return;

                    // Step 2: Choose the novel with the same title and author
                    var trimmedTitle = novel.Title.Trim();
                    var sameNovel = searchResults.FirstOrDefault(n => (n.Title.Trim() == trimmedTitle));
                    if (sameNovel != null)
                    {
                        while (!novels.TryAdd(plugin.Name, sameNovel)) ;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error when searching for novels in {plugin.Name}");
                }
            }
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

            // Using threads parallel to search for the chapter in other sources, each thread for each plugin
            var tasks = novelInOtherSources.Select(kvp => Task.Run(async () =>
            {
                var otherSource = kvp.Key;
                var otherNovel = kvp.Value;

                // Skip if the source is the same or the novel slug is null
                if (otherSource == thisSource || otherNovel.Slug == null) return;

                // Get the plugin in the Installed list
                var otherPlugin = Installed.Find(p => p.Name == otherSource);
                if (otherPlugin == null) return;

                // Execute the plugin
                if (otherPlugin.PluginInstance is ISourcePlugin executablePlugin)
                {
                    try
                    {
                        // Search for the chapter with the same number
                        var otherChapter = await executablePlugin.GetChapterAddrByNumber(otherNovel.Slug, thisChapterNumber);
                        if (otherChapter != null)
                        {
                            otherChapter.Source = otherSource;
                            lock (chapters)
                            {
                                chapters.Add(otherSource, otherChapter);
                            }
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error when searching for chapters in {otherSource}");
                    }
                }
            })).ToArray();

            // Wait for all tasks to finish
            await Task.WhenAll(tasks);

            // Ensure to stop every tasks
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                {
                    _logger.LogError(task.Exception?.InnerException, "Error when searching for chapters in other sources");
                }
            }

            // If no chapter is found, return null
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
            // Caching the categories
            var cacheKey = $"categories-{source}";
            if (_cacheService.TryGetValue(cacheKey, out Category[] categories))
            {
                _logger.LogInformation($"Cache hit for categories of {source}");
                return categories;
            }

            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the list of categories
            //Category[]? categories = null;
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                categories = await executablePlugin.CrawlCategories();
            }

            if (categories == null)
            {
                categories = Array.Empty<Category>();
            }

            // Cache the categories
            _cacheService.Set(cacheKey, categories, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                SlidingExpiration = TimeSpan.FromMinutes(30),
                Size = 1
            });

            return categories;
        }

        public async Task<Tuple<Novel[], int>> GetNovelsByCategory(string source, string categorySlug, int page = 1)
        {
            Novel[]? novels = null;
            int totalPage = -1;

            // Caching the novels for page 1
            if (page == 1)
            {
                var cacheKey = $"novels-{source}-{categorySlug}-page1";
                var cacheKeyTotalPage = $"novels-{source}-{categorySlug}-totalPage";

                if (_cacheService.TryGetValue(cacheKey, out novels) && _cacheService.TryGetValue(cacheKeyTotalPage, out totalPage))
                {
                    _logger.LogInformation($"Cache hit for novels of {source} in category {categorySlug} at page 1");
                    return new Tuple<Novel[], int>(novels, totalPage);
                }
            }

            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the novels by category
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlByCategory(categorySlug, page);
            }

            if (novels == null)
            {
                novels = Array.Empty<Novel>();
            }

            // Cache the novels for page 1
            if (page == 1)
            {
                var cacheKey = $"novels-{source}-{categorySlug}-page1";
                var cacheKeyTotalPage = $"novels-{source}-{categorySlug}-totalPage";

                _cacheService.Set(cacheKey, novels, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });
                _cacheService.Set(cacheKeyTotalPage, totalPage, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });
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
            Novel[]? novels = null;
            int totalPage = -1;

            // Cache the hot novels for page 1
            if (page == 1)
            {
                var cacheKey = $"hot-novels-{source}-page1";
                var cacheKeyTotalPage = $"hot-novels-{source}-totalPage";

                if (_cacheService.TryGetValue(cacheKey, out novels) && _cacheService.TryGetValue(cacheKeyTotalPage, out totalPage))
                {
                    _logger.LogInformation($"Cache hit for hot novels of {source} at page 1");
                    return new Tuple<Novel[], int>(novels, totalPage);
                }
            }

            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the hot novels
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlHot(page);
            }

            if (novels == null)
            {
                novels = Array.Empty<Novel>();
            }

            // Cache the hot novels for page 1
            if (page == 1)
            {
                var cacheKey = $"hot-novels-{source}-page1";
                var cacheKeyTotalPage = $"hot-novels-{source}-totalPage";

                _cacheService.Set(cacheKey, novels, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });
                _cacheService.Set(cacheKeyTotalPage, totalPage, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });
            }

            return new Tuple<Novel[], int>(novels, totalPage);
        }

        public async Task<Tuple<Novel[], int>> GetLatestNovels(string source, int page = 1)
        {
            Novel[]? novels = null;
            int totalPage = -1;

            // Cache the latest novels for page 1
            if (page == 1)
            {
                var cacheKey = $"latest-novels-{source}-page1";
                var cacheKeyTotalPage = $"latest-novels-{source}-totalPage";

                if (_cacheService.TryGetValue(cacheKey, out novels) && _cacheService.TryGetValue(cacheKeyTotalPage, out totalPage))
                {
                    _logger.LogInformation($"Cache hit for latest novels of {source} at page 1");
                    return new Tuple<Novel[], int>(novels, totalPage);
                }
            }

            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the latest novels
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlLatest(page);
            }

            if (novels == null)
            {
                novels = Array.Empty<Novel>();
            }

            // Cache the latest novels for page 1
            if (page == 1)
            {
                var cacheKey = $"latest-novels-{source}-page1";
                var cacheKeyTotalPage = $"latest-novels-{source}-totalPage";

                _cacheService.Set(cacheKey, novels, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });
                _cacheService.Set(cacheKeyTotalPage, totalPage, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });
            }

            return new Tuple<Novel[], int>(novels, totalPage);
        }

        public async Task<Tuple<Novel[], int>> GetCompletedNovels(string source, int page = 1)
        {
            Novel[]? novels = null;
            int totalPage = -1;

            // Cache the completed novels for page 1
            if (page == 1)
            {
                var cacheKey = $"completed-novels-{source}-page1";
                var cacheKeyTotalPage = $"completed-novels-{source}-totalPage";

                if (_cacheService.TryGetValue(cacheKey, out novels) && _cacheService.TryGetValue(cacheKeyTotalPage, out totalPage))
                {
                    _logger.LogInformation($"Cache hit for completed novels of {source} at page 1");
                    return new Tuple<Novel[], int>(novels, totalPage);
                }
            }

            // Get the plugin in the Installed list
            var plugin = Installed.Find(p => p.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the completed novels
            if (plugin.PluginInstance is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlCompleted(page);
            }

            if (novels == null)
            {
                novels = Array.Empty<Novel>();
            }

            // Cache the completed novels for page 1
            if (page == 1)
            {
                var cacheKey = $"completed-novels-{source}-page1";
                var cacheKeyTotalPage = $"completed-novels-{source}-totalPage";

                _cacheService.Set(cacheKey, novels, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });
                _cacheService.Set(cacheKeyTotalPage, totalPage, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });
            }

            return new Tuple<Novel[], int>(novels, totalPage);
        }



    }
}
