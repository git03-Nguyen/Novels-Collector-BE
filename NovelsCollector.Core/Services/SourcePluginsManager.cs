using MongoDB.Driver;
using NovelsCollector.Core.Exceptions;
using NovelsCollector.Core.Models;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace NovelsCollector.Core.Services
{
    public class SourcePluginsManager
    {
        #region Properties

        private readonly ILogger<SourcePluginsManager> _logger;

        // Storing the plugins and their own contexts
        public List<SourcePlugin> Installed { get; }

        // The path to the /source-plugins and /temp folders
        private readonly string _pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "source-plugins");
        private readonly string _tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");

        // The collection of source-plugins in the database
        private IMongoCollection<SourcePlugin> _pluginsCollection;

        // FOR DEBUGGING: The list of weak references to the unloaded contexts in the past
        public List<WeakReference> unloadedHistory = new List<WeakReference>();

        #endregion

        /// <summary>
        /// The constructor of the SourcePluginsManager class.
        /// </summary>
        /// <param name="logger"> The logger service, from Dependency Injection. </param>
        /// <param name="mongoDbContext"> The MongoDB context, from Dependency Injection. </param>
        public SourcePluginsManager(ILogger<SourcePluginsManager> logger, MongoDbContext mongoDbContext)
        {
            _logger = logger;
            _pluginsCollection = mongoDbContext.SourcePlugins;

            // Get installed plugins from the database
            Installed = _pluginsCollection.Find(plugin => true).ToList();

            // Create the /source-plugins and /temp folders if not exist
            if (!Directory.Exists(_pluginsPath)) Directory.CreateDirectory(_pluginsPath);
            if (!Directory.Exists(_tempPath)) Directory.CreateDirectory(_tempPath);

            // Load all installed plugins
            loadAll();
        }

        /// <summary>
        /// Load all installed plugins.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // Avoid JIT optimizations that may cause issues with the PluginLoadContext.Unload() (cannot GC)
        private void loadAll()
        {
            unloadAll();
            foreach (var plugin in Installed)
            {
                if (plugin.Name != null && plugin.PluginInstance == null)
                {
                    try
                    {
                        LoadPlugin(plugin.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error loading plugin {plugin.Name}");
                    }
                }
            }
        }

        /// <summary>
        /// Unload all loaded plugins.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void unloadAll()
        {
            foreach (var plugin in Installed)
            {
                if (plugin.Name != null && plugin.PluginInstance != null)
                {
                    try
                    {
                        UnloadPlugin(plugin.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error unloading plugin {plugin.Name}");
                    }
                }
            }

            // count how many plugins having .PluginInstance != null
            int countStillLoaded = Installed.Count(plugin => plugin.PluginInstance != null);
            if (countStillLoaded > 0)
            {
                _logger.LogError($"{countStillLoaded} plugins are not unloaded properly");
            }
            else
            {
                _logger.LogInformation("All plugins are unloaded successfully");
            }

            //GC.Collect();
            //GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Load a plugin by name.
        /// </summary>
        /// <param name="pluginName"> The name of the plugin to load. </param>
        /// <exception cref="NotFoundException"> Plugin not found. </exception>
        /// <exception cref="Exception"> Plugin has been already loaded. </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LoadPlugin(string pluginName)
        {
            // Find the plugin in the Installed list
            SourcePlugin? loadingPlugin = Installed.Find(p => p.Name == pluginName);

            // If the plugin is not found or already loaded, throw an exception
            if (loadingPlugin == null)
                throw new NotFoundException("Plugin not found");
            if (loadingPlugin.PluginInstance != null)
                throw new Exception("Plugin has been already loaded");

            // If the plugin is not loaded, load it

            // Path to the plugin dll: source-plugins/{pluginName}/{loadingPlugin.Assembly} .e.g: source-plugins/{pluginName}/Source.{pluginName}.dll
            string pluginNameFolder = Path.Combine(_pluginsPath, pluginName);
            if (!Directory.Exists(pluginNameFolder))
                throw new NotFoundException($"Plugin folder /source-plugins/{pluginName} not found");
            string assemblyName = loadingPlugin.Assembly;
            if (!assemblyName.EndsWith(".dll")) assemblyName += ".dll";
            string? pluginDll = Directory.GetFiles(pluginNameFolder, $"{assemblyName}").FirstOrDefault();
            if (pluginDll == null)
                throw new NotFoundException($"Assembly {assemblyName} not found");

            // Create a new context to load the plugin into
            PluginLoadContext loadContext = new PluginLoadContext(pluginDll);

            // Load the plugin assembly: {plugin.Assembly} .e.g: Source.{pluginName}.dll
            Assembly pluginAssembly = loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(pluginDll));
            Type[] types = pluginAssembly.GetTypes();
            foreach (var type in types)
            {
                // if the type implementing the ISourcePlugin interface, create an instance of it
                if (type.GetInterface(nameof(ISourcePlugin)) != null)
                {
                    var plugin = Activator.CreateInstance(type) as ISourcePlugin;
                    if (plugin != null)
                    {
                        // Load the plugin
                        loadingPlugin.PluginInstance = plugin;
                        loadingPlugin.IsLoaded = true;
                        loadingPlugin.LoadContext = loadContext;
                        _logger.LogInformation($"\tLOADED plugin {pluginName} from {assemblyName}");
                        return;
                    }
                }
            }

            // If there is no plugin SourcePlugin loaded, cancel the loading process
            loadContext.Unload();
            loadingPlugin.PluginInstance = null;
            loadingPlugin.IsLoaded = false;
            loadingPlugin.LoadContext = null;
            throw new NotFoundException($"No plugin found in {assemblyName}");

        }

        /// <summary>
        /// Unload a plugin by name.
        /// </summary>
        /// <param name="pluginName"> The name of the plugin to unload. </param>
        /// <exception cref="NotFoundException"> Plugin not found. </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UnloadPlugin(string pluginName)
        {
            // Find the plugin in the Installed list
            SourcePlugin? unloadingPlugin = Installed.Find(p => p.Name == pluginName);

            // If the plugin is not found or already unloaded, throw an exception
            if (unloadingPlugin == null)
                throw new NotFoundException("Plugin not found");
            if (unloadingPlugin.PluginInstance == null)
                throw new Exception("Plugin has been already unloaded");


            // FOR DEBUGGING: Add the loaded context to the history
            unloadedHistory.Add(new WeakReference(unloadingPlugin.LoadContext));

            // If the plugin is loaded, unload it
            unloadingPlugin.LoadContext?.Unload();
            unloadingPlugin.PluginInstance = null;
            unloadingPlugin.IsLoaded = false;
            unloadingPlugin.LoadContext = null;
            _logger.LogInformation($"\tUNLOADING plugin {pluginName} has been initiated");

        }

        /// <summary>
        /// Add a new plugin from a file.
        /// </summary>
        /// <param name="file"> The file to add as a plugin, in .zip format. </param>
        /// <returns> The name of the plugin added. </returns>
        /// <exception cref="NotFoundException"> Manifest file not found. </exception>
        /// <exception cref="Exception"> Plugin already exists. </exception>
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
            File.Delete(pluginZipPath);

            // Delete the .pdb file - they are not needed
            string[] pdbFiles = Directory.GetFiles(tempPath, "*.pdb");
            foreach (var pdbFile in pdbFiles) File.Delete(pdbFile);

            // Read the plugin name, author, version, etc. from the manifest file
            string manifestPath = Path.Combine(tempPath, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new NotFoundException("Manifest file not found");

            // Get the new plugin
            string manifestContent = File.ReadAllText(manifestPath);
            var newPlugin = JsonSerializer.Deserialize<SourcePlugin>(manifestContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });
            if (newPlugin == null)
                throw new NotFoundException("Manifest file is invalid");

            // Check if the plugin already exists
            if (Installed.Any(plugin => plugin.Name == newPlugin.Name))
                throw new Exception("Plugin already exists");

            // Add the plugin to the Installed list
            Installed.Add(newPlugin);

            // Move the plugin to the plugins folder
            string pluginPath = Path.Combine(_pluginsPath, newPlugin.Name);
            if (Directory.Exists(pluginPath)) Directory.Delete(pluginPath, true);
            Directory.Move(tempPath, pluginPath);

            // Try to load the plugin
            try
            {
                LoadPlugin(newPlugin.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading plugin {newPlugin.Name}");
                Installed.Remove(newPlugin);
                Directory.Delete(pluginPath, true);
                throw;
            }

            // If loaded successfully, add the plugin to the database
            await _pluginsCollection.InsertOneAsync(newPlugin);

            _logger.LogInformation($"\tPlugin {newPlugin.Name} ADDED successfully");
            return newPlugin.Name;
        }

        public async void RemovePlugin(string pluginName)
        {
            // Find the plugin in the Installed list
            SourcePlugin? removingPlugin = Installed.Find(p => p.Name == pluginName);

            // If the plugin is not found, throw an exception
            if (removingPlugin == null)
                throw new NotFoundException("Plugin not found");

            // If the plugin is loaded, unload it
            if (removingPlugin.PluginInstance != null)
            {
                try
                {
                    UnloadPlugin(pluginName);
                    // Ensure the plugin is unloaded
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                catch (Exception ex)
                {
                    throw new Exception("Error unloading plugin", ex);
                }
            }

            // Remove the plugin from the Installed list
            Installed.Remove(removingPlugin);

            // Remove the plugin from the database
            await _pluginsCollection.DeleteOneAsync(plugin => plugin.Name == pluginName);

            // Delete the plugin folder
            string pluginPath = Path.Combine(_pluginsPath, pluginName);
            if (Directory.Exists(pluginPath)) Directory.Delete(pluginPath, true);

            _logger.LogInformation($"\tPlugin {pluginName} REMOVED successfully");
        }

        // -------------- MANAGE FOR SOURCE PLUGINS --------------
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
                // else if search by keyword, do nothing
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
