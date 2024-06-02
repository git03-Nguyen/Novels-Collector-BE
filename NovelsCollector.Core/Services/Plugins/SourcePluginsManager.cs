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
        // A dictionary to store the plugins
        public Dictionary<string, SourcePlugin> Plugins { get; } = new Dictionary<string, SourcePlugin>();

        // A dictionary to store the plugin contexts
        private Dictionary<string, PluginLoadContext> _pluginLoadContexts = new Dictionary<string, PluginLoadContext>();

        // The path to the plugins folder
        private string _pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

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

        // FOR TESTING: Weak reference to the contexts
        public List<WeakReference> weakRefsToContexts = new List<WeakReference>();


        public SourcePluginsManager(MongoDbContext mongoDbContext)
        {
            _pluginsCollection = mongoDbContext.SourcePlugins;
            Reload();
        }

        // No inlining to avoid JIT optimizations that may cause issues with the PluginLoadContext.Unload() (cannot GC)
        [MethodImpl(MethodImplOptions.NoInlining)]    
        public void LoadPluginIntoContext(string pluginName)
        {
            //if (!Plugins.ContainsKey(pluginName)) return;
            if (!Directory.Exists(_pluginsPath)) return;

            Plugins.Clear();
            _pluginLoadContexts.Clear();
            weakRefsToContexts.Clear();

            string pluginPath = Path.Combine(_pluginsPath, pluginName); // Path to the plugin folder. ie: Plugins/TruyenFullVn
            string? pathToDll = Directory.GetFiles(pluginPath, $"Source.{pluginName}.dll").FirstOrDefault(); // Path to the plugin dll. ie: Plugins/TruyenFullVn/Source.TruyenFullVn.dll
            if (pathToDll == null) return;

            Console.WriteLine($"Loading plugin from {pathToDll}");

            PluginLoadContext loadContext = new PluginLoadContext(pathToDll);
            weakRefsToContexts.Add(new WeakReference(loadContext));

            Assembly pluginAssembly = loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(pathToDll));
            Type[] types = pluginAssembly.GetTypes();
            foreach (var type in types)
            {
                if (typeof(SourcePlugin).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var plugin = (SourcePlugin)Activator.CreateInstance(type);
                    if (plugin == null) continue;
                    Plugins.Add(pluginName, plugin);
                    _pluginLoadContexts.Add(pluginName, loadContext);
                }
            }

            Console.WriteLine($"Plugin {pluginName} loaded successfully");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UnloadPlugin(string pluginName)
        {
            //if (!Plugins.ContainsKey(pluginName)) return;
            //if (!_pluginLoadContexts.ContainsKey(pluginName)) return;

            Console.WriteLine($"Unloading plugin {pluginName}");

            _pluginLoadContexts[pluginName].Unload();
            _pluginLoadContexts.Clear();

            Plugins.Remove(pluginName);

            Console.WriteLine($"Plugin {pluginName} started to unload");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WaitingForGC()
        {
            // Poll and run GC until the AssemblyLoadContext is unloaded.
            // You don't need to do that unless you want to know when the context
            // got unloaded. You can just leave it to the regular GC.
            for (int i = 0; weakRefsToContexts[0].IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Console.WriteLine($"Unload success: {!weakRefsToContexts[0].IsAlive}");
        }

        // ----------------- OLD CODE -----------------
        public void LoadPlugin(string pluginName)
        {
            if (Plugins.ContainsKey(pluginName)) return;

            string pluginPath = Path.Combine(_pluginsPath, $"{pluginName}");
            if (!Directory.Exists(pluginPath)) return;

            //var dlls = Directory.GetFiles(pluginPath, "*.dll");
            var dll = Directory.GetFiles(pluginPath, "*.dll").FirstOrDefault();

            if (!File.Exists(dll)) return;

            Assembly assembly = Assembly.LoadFile(dll);
            Type[] types = assembly.GetTypes();

            foreach (var type in types)
            {
                if (typeof(SourcePlugin).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var plugin = (SourcePlugin)Activator.CreateInstance(type);
                    if (plugin == null) continue;
                    Plugins.Add(pluginName, plugin);
                }
            }
        }

        public void Reload()
        {
            Console.WriteLine("Reloading plugins...");
            // print the collection of plugins
            //var all_plugins = _pluginsCollection.Find(_ => true).ToList();
            //foreach (var plugin in all_plugins)
            //{
            //    Console.WriteLine(plugin.Name);
            //}
            Plugins.Clear();

            if (!Directory.Exists(_pluginsPath))
            {
                Directory.CreateDirectory(_pluginsPath);
            }

            // For test:
            var listOfEnabledPlugins = new List<string> { "TruyenFullVn", "TruyenTangThuVienVn", "SSTruyenVn", "DTruyenCom" }; // in the future, read from db
            foreach (var plugin in listOfEnabledPlugins)
            {
                LoadPlugin(plugin);
            }
        }

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

        public async Task Add(SourcePlugin plugin)
        {
            //if (_plugins.ContainsKey(plugin.))
            //{
            //    throw new Exception("Plugin already exists");
            //}

            //string pluginPath = Path.Combine(_pluginsPath, $"{plugin.Name}");
            //if (Directory.Exists(pluginPath))
            //{
            //    throw new Exception("Plugin folder already exists");
            //}
            //if (!Directory.Exists(pluginPath)) Directory.CreateDirectory(pluginPath);

            //await _pluginsCollection.InsertOneAsync(plugin);

        }

    }
}
