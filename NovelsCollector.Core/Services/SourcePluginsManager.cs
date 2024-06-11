using MongoDB.Driver;
using NovelsCollector.Core.Exceptions;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace NovelsCollector.Core.Services
{
    public class SourcePluginsManager
    {
        private readonly ILogger<SourcePluginsManager> _logger;

        // 2 dictionaries to store the plugins and their own contexts
        public Dictionary<string, SourcePlugin> Plugins { get; } = new Dictionary<string, SourcePlugin>();
        private Dictionary<string, PluginLoadContext> _pluginLoadContexts = new Dictionary<string, PluginLoadContext>();

        // The path to the plugins folder
        private readonly string _pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "source-plugins");
        private readonly string _tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");

        // TODO: The collection of source-plugins in the database
        private IMongoCollection<SourcePlugin> _pluginsCollection = null;

        // FOR TESTING: The list of installed plugins
        private List<string> _enabledPlugins = new List<string>
        {
            "TruyenFullVn",
            "TruyenTangThuVienVn",
            //"SSTruyenVn",
            "DTruyenCom"
        };

        string maninfest = @"
            'Name': 'SSTruyenVN',
            'Description': 'Đây là plugin crawl truyện từ trang sstruyen.vn',
            'Version': '1.0.0',
            'Author': 'Nguyễn Tuấn Đạt',
            'Url': 'https://sstruyen.vn',
        ";

        // FOR DEBUGGING: The list of weak references to the unloaded contexts
        public List<WeakReference> unloadedContexts = new List<WeakReference>();

        public SourcePluginsManager(ILogger<SourcePluginsManager> logger, MongoDbContext mongoDbContext)
        {
            _logger = logger;
            _pluginsCollection = mongoDbContext.SourcePlugins;

            if (!Directory.Exists(_pluginsPath)) Directory.CreateDirectory(_pluginsPath);
            if (!Directory.Exists(_tempPath)) Directory.CreateDirectory(_tempPath);

            // Load all installed plugins
            loadAll();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void unloadAll()
        {
            // FOR DEBUGGING: Clear the history of unloaded contexts
            unloadedContexts.Clear();

            if (Plugins.Count > 0 || _pluginLoadContexts.Count > 0)
            {
                foreach (var plugin in Plugins)
                {
                    UnloadPlugin(plugin.Key);
                }
            }

            Plugins.Clear();
            _pluginLoadContexts.Clear();
            _logger.LogInformation("\tAll plugins unloaded");

            //GC.Collect();
            //GC.WaitForPendingFinalizers();
        }

        // Avoid JIT optimizations that may cause issues with the PluginLoadContext.Unload() (cannot GC)
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void loadAll()
        {
            unloadAll();
            foreach (var plugin in _enabledPlugins)
            {
                LoadPlugin(plugin);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool LoadPlugin(string pluginName)
        {
            // Check if the plugin is already loaded
            if (Plugins.ContainsKey(pluginName))
            {
                _logger.LogError($"\tPlugin {pluginName} already loaded");
                return false;
            }

            // Path to the plugin folder. ie: Plugins/{pluginName}
            string pluginPath = Path.Combine(_pluginsPath, pluginName);

            // Path to the plugin dll. ie: Plugins/{pluginName}/Source.{pluginName}.dll
            string? pathToDll = Directory.GetFiles(pluginPath, $"Source.{pluginName}.dll").FirstOrDefault();
            if (pathToDll == null)
            {
                _logger.LogError($"\tPlugin \"Source.{pluginName}.dll\" not found");
                return false;
            }

            // Create a new context to load the plugin into
            _logger.LogInformation($"\tLOADING {pluginName} from /source-plugins/{pluginName}");
            PluginLoadContext loadContext = new PluginLoadContext(pathToDll);

            // Load the plugin assembly ("Source.{pluginName}.dll")
            Assembly pluginAssembly = loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(pathToDll));
            Type[] types = pluginAssembly.GetTypes();
            var hasSourcePlugin = false;
            foreach (var type in types)
            {
                if (typeof(SourcePlugin).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var plugin = Activator.CreateInstance(type) as SourcePlugin;
                    if (plugin == null) continue;
                    // Add the plugin (SourcePlugin) to the dictionary => each assembly must have only 1 SourcePlugin
                    Plugins.Add(pluginName, plugin);
                    hasSourcePlugin = true;
                    break;
                }
            }

            // If there is no plugin SourcePlugin loaded, cancel the loading process
            if (!hasSourcePlugin)
            {
                _logger.LogError($"\tNo SourcePlugin found in Source.{pluginName}.dll");
                loadContext.Unload();
                return false;
            }

            // Else, successfully, add the context to the dictionary
            _pluginLoadContexts.Add(pluginName, loadContext);
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UnloadPlugin(string pluginName)
        {
            if (!Plugins.ContainsKey(pluginName)
                || !_pluginLoadContexts.ContainsKey(pluginName))
            {
                _logger.LogWarning($"\tCannot unload {pluginName} because it wasn't loaded");
            }

            // Unload the plugin
            _logger.LogInformation($"\tUNLOADING plugin {pluginName}");

            // Initiate the unloading process
            unloadedContexts.Add(new WeakReference(_pluginLoadContexts[pluginName])); // FOR DEBUGGING
            _pluginLoadContexts[pluginName].Unload();

            // Remove all references, except the weak reference
            Plugins.Remove(pluginName);
            _pluginLoadContexts.Remove(pluginName);
        }

        public async Task<string> AddPluginFromFile(IFormFile file)
        {
            // Get the timestamp to create a unique folder
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            // Download the plugin into the folder
            string pluginZipPath = Path.Combine(_tempPath, timestamp + ".zip");
            string tempPath = Path.Combine(_tempPath, timestamp);
            Directory.CreateDirectory(tempPath);

            // Download the plugin
            _logger.LogInformation($"\tDownloading new plugin");
            using (var stream = new FileStream(pluginZipPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Extract the plugin
            _logger.LogInformation($"\tExtracting plugin to /temp/{timestamp}");
            ZipFile.ExtractToDirectory(pluginZipPath, tempPath);
            // Delete the zip file
            File.Delete(pluginZipPath);

            // Read the plugin name, author, version, etc. from the manifest file: parse an object
            string manifestPath = Path.Combine(tempPath, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new NotFoundException("Manifest file not found");

            string manifestContent = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestContent);
            if (manifest == null)
                throw new NotFoundException("Manifest file is invalid");


            string pluginName = manifest["Name"];
            if (Plugins.ContainsKey(pluginName) || _enabledPlugins.Contains(pluginName))
            {
                throw new Exception("Plugin already exists");
            }

            // Move the plugin to the plugins folder
            string pluginPath = Path.Combine(_pluginsPath, pluginName);
            Directory.Move(tempPath, pluginPath);

            // Load the plugin
            //_enabledPlugins.Add(pluginName);
            //LoadPlugin(pluginName);

            // Save the plugin to the database
            //var plugin = new SourcePlugin
            //{
            //    Name = pluginName,
            //    DownloadUrl = downloadUrl
            //};
            //await _pluginsCollection.InsertOneAsync(plugin);

            _logger.LogInformation($"\tPlugin {pluginName} added successfully");
            return pluginName;
        }

        public async void RemovePlugin(string pluginName)
        {
            if (!_enabledPlugins.Contains(pluginName))
            {
                _logger.LogWarning($"\tPlugin {pluginName} not found");
                throw new NotFoundException("Plugin not found");
            }

            // Delete the plugin folder
            string pluginPath = Path.Combine(_pluginsPath, pluginName);
            if (Directory.Exists(pluginPath))
            {
                Directory.Delete(pluginPath, true);
            }

            // Remove the plugin from the list
            _enabledPlugins.Remove(pluginName);
            UnloadPlugin(pluginName);

            // Remove the plugin from the database
            //await _pluginsCollection.DeleteOneAsync(plugin => plugin.Name == pluginName);

            _logger.LogInformation($"\tPlugin {pluginName} removed successfully");
        }

        // -------------- MANAGE FOR SOURCE PLUGINS --------------
        public async Task<Tuple<Novel[]?, int>> Search(string source, string? keyword, string? title, string? author, int page = 1)
        {
            if (Plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            if (!Plugins.ContainsKey(source))
            {
                throw new Exception("Source not found");
            }

            if (keyword == null && title == null && author == null)
            {
                throw new Exception("No query found");
            }

            var plugin = Plugins[source];
            Novel[]? novels = null;
            int totalPage = -1;

            if (plugin is ISourcePlugin executablePlugin)
            {
                string query = keyword ?? author ?? title ?? "";
                (novels, totalPage) = await executablePlugin.CrawlSearch(query, page);

                // filter if search by title
                if (query == title)
                {
                    novels = novels?.Where(novel => novel.Title.ToLower().Contains(title.ToLower())).ToArray();
                }
                // filter if search by author
                else if (query == author)
                {
                    novels = novels?.Where(novel => novel.Authors[0]?.Name.ToLower().Contains(author.ToLower()) ?? false).ToArray();
                }
            }

            if (novels == null)
            {
                throw new Exception("No result found");
            }

            return new Tuple<Novel[]?, int>(novels, totalPage);
        }

        public async Task<Novel?> GetNovelDetail(string source, string novelSlug)
        {
            if (Plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            if (!Plugins.ContainsKey(source))
            {
                throw new Exception("Source not found");
            }

            var plugin = Plugins[source];

            Novel? novel = null;

            if (plugin is ISourcePlugin executablePlugin)
            {
                novel = await executablePlugin.CrawlDetail(novelSlug);
            }

            if (novel == null)
            {
                throw new Exception("No result found");
            }

            return novel;
        }

        public async Task<Dictionary<string, Novel>?> GetNovelFromOtherSources(string excludedSource, Novel novel)
        {
            Dictionary<string, Novel> novels = new Dictionary<string, Novel>();

            foreach (var plugin in Plugins)
            {
                if (plugin.Key == excludedSource) continue;

                var otherPlugin = plugin.Value;
                if (otherPlugin is ISourcePlugin executablePlugin)
                {
                    // search by title
                    var (searchResults, _) = await executablePlugin.CrawlSearch(novel.Title, 1);
                    if (novel == null) continue;

                    // choose the novel with the same title and author
                    var sameNovel = searchResults?.FirstOrDefault(n => n.Title == novel.Title && n.Authors[0]?.Name == novel.Authors[0]?.Name);
                    if (sameNovel != null)
                    {
                        novels.Add(plugin.Key, sameNovel);
                    }
                }
            }

            if (novels.Count == 0) return null;

            // only return the title and slug of each novel
            return novels.ToDictionary(kvp => kvp.Key, kvp => new Novel
            {
                Title = kvp.Value.Title,
                Slug = kvp.Value.Slug
            });
        }

        public async Task<Dictionary<string, Chapter>?> GetChapterFromOtherSources(Dictionary<string, Novel> novelInOtherSources, Chapter currentChapter)
        {
            if (currentChapter.Source == null || currentChapter.NovelSlug == null || currentChapter.Number == null ||
                novelInOtherSources.Count == 0)
            {
                return null;
            }

            Dictionary<string, Chapter> chapters = new Dictionary<string, Chapter>();
            string thisSource = currentChapter.Source;
            int thisChapterNumber = currentChapter.Number.Value;

            foreach (var novel in novelInOtherSources)
            {
                var otherSource = novel.Key;
                var otherNovel = novel.Value;

                if (otherSource == thisSource || otherNovel.Slug == null) continue;

                var otherPlugin = Plugins[otherSource];
                if (otherPlugin is ISourcePlugin executablePlugin)
                {
                    // search for the chapter with the same number
                    var otherChapter = await executablePlugin.GetChapterSlug(otherNovel.Slug, thisChapterNumber);
                    if (otherChapter != null)
                    {
                        chapters.Add(otherSource, otherChapter);
                    }
                }
            }

            if (chapters.Count == 0) return null;
            return chapters.ToDictionary(chapter => chapter.Key, chapter => chapter.Value);
        }

        public async Task<Tuple<Chapter[]?, int>> GetChapters(string source, string novelSlug, int page = -1)
        {
            if (Plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            if (!Plugins.ContainsKey(source))
            {
                throw new Exception("Source not found");
            }

            var plugin = Plugins[source];

            Chapter[]? chapters = null;
            int totalPage = -1;

            if (plugin is ISourcePlugin executablePlugin)
            {
                (chapters, totalPage) = await executablePlugin.CrawlListChapters(novelSlug, page);
            }

            if (chapters == null)
            {
                throw new Exception("No result found");
            }

            return new Tuple<Chapter[]?, int>(chapters, totalPage);
        }

        public async Task<Chapter?> GetChapter(string source, string novelSlug, string chapterSlug)
        {
            if (Plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            if (!Plugins.ContainsKey(source))
            {
                throw new Exception("Source not found");
            }

            var plugin = Plugins[source];

            Chapter? chapter = null;
            if (plugin is ISourcePlugin executablePlugin)
            {
                chapter = await executablePlugin.CrawlChapter(novelSlug, chapterSlug);
            }

            if (chapter == null)
            {
                throw new Exception("No result found");
            }

            return chapter;
        }

        public async Task<Category[]> GetCategories(string source)
        {
            if (Plugins.Count == 0) throw new Exception("No plugins loaded");

            if (!Plugins.ContainsKey(source)) throw new Exception("Source not found");

            var plugin = Plugins[source];

            Category[]? categories = null;
            if (plugin is ISourcePlugin executablePlugin)
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
            if (Plugins.Count == 0) throw new Exception("No plugins loaded");

            if (!Plugins.ContainsKey(source)) throw new Exception("Source not found");

            var plugin = Plugins[source];

            Novel[]? novels = null;
            int totalPage = -1;
            if (plugin is ISourcePlugin executablePlugin)
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
            if (Plugins.Count == 0) throw new Exception("No plugins loaded");

            if (!Plugins.ContainsKey(source)) throw new Exception("Source not found");

            var plugin = Plugins[source];

            Novel[]? novels = null;
            int totalPage = -1;
            if (plugin is ISourcePlugin executablePlugin)
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
            if (Plugins.Count == 0) throw new Exception("No plugins loaded");

            if (!Plugins.ContainsKey(source)) throw new Exception("Source not found");

            var plugin = Plugins[source];

            Novel[]? novels = null;
            int totalPage = -1;
            if (plugin is ISourcePlugin executablePlugin)
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
            if (Plugins.Count == 0) throw new Exception("No plugins loaded");

            if (!Plugins.ContainsKey(source)) throw new Exception("Source not found");

            var plugin = Plugins[source];

            Novel[]? novels = null;
            int totalPage = -1;
            if (plugin is ISourcePlugin executablePlugin)
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
            if (Plugins.Count == 0) throw new Exception("No plugins loaded");

            if (!Plugins.ContainsKey(source)) throw new Exception("Source not found");

            var plugin = Plugins[source];

            Novel[]? novels = null;
            int totalPage = -1;
            if (plugin is ISourcePlugin executablePlugin)
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
