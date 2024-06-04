using MongoDB.Driver;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NovelsCollector.Core.Services.Plugins
{
    public class SourcePluginsManager
    {
        private readonly ILogger<SourcePluginsManager> _logger;

        // 2 dictionaries to store the plugins and their own contexts
        public Dictionary<string, SourcePlugin> Plugins { get; } = new Dictionary<string, SourcePlugin>();
        private Dictionary<string, PluginLoadContext> _pluginLoadContexts = new Dictionary<string, PluginLoadContext>();

        // The path to the plugins folder
        private readonly string _pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        // TODO: The collection of source-plugins in the database
        private IMongoCollection<SourcePlugin> _pluginsCollection = null;

        // FOR TESTING: The list of installed plugins
        private List<string> _installedPlugins = new List<string>
        {
            "TruyenFullVn",
            "TruyenTangThuVienVn",
            "SSTruyenVn",
            "DTruyenCom"
        };

        // FOR DEBUGGING: The list of weak references to the unloaded contexts
        public List<WeakReference> unloadedContexts = new List<WeakReference>();

        public SourcePluginsManager(ILogger<SourcePluginsManager> logger, MongoDbContext mongoDbContext)
        {
            _logger = logger;
            _pluginsCollection = mongoDbContext.SourcePlugins;

            if (!Directory.Exists(_pluginsPath))
            {
                Directory.CreateDirectory(_pluginsPath);
            }

            // Load all installed plugins
            ReloadPlugins();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UnloadAll()
        {
            // FOR DEBUGGING: Clear the list of unloaded contexts
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
        public void ReloadPlugins()
        {
            UnloadAll();
            foreach (var plugin in _installedPlugins)
            {
                LoadPlugin(plugin);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool LoadPlugin(string pluginName)
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
            _logger.LogInformation($"\tLOADING {pluginName} from /Plugins/{pluginName}");
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

        // -------------- MANAGE FOR SOURCE PLUGINS --------------
        public async Task<Tuple<Novel[]?, int>> Search(string source, string keyword, string? author, string? year, int page = 1)
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
            Novel[]? novels = null;
            int totalPage = -1;

            if (plugin is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlSearch(keyword: keyword, page: page);
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



    }
}
