using MongoDB.Driver;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.Reflection;

namespace NovelsCollector.Core.Services.Plugins
{
    public class SourcePluginsManager
    {
        // A dictionary to store the plugins
        private static readonly Dictionary<string, SourcePlugin> _plugins = new Dictionary<string, SourcePlugin>();

        // The path to the plugins folder
        private static readonly string _pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        // The collection of plugins in the database
        private static IMongoCollection<SourcePlugin> _pluginsCollection = null;

        public Dictionary<string, SourcePlugin> Plugins => _plugins;

        public SourcePluginsManager(MongoDbContext mongoDbContext)
        {
            _pluginsCollection = mongoDbContext.SourcePlugins;
            Reload();
        }

        public void LoadPlugin(string pluginName)
        {
            if (_plugins.ContainsKey(pluginName)) return;

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
                    _plugins.Add(pluginName, plugin);
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
            _plugins.Clear();

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
            if (_plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            if (!_plugins.ContainsKey(source))
            {
                throw new Exception("Source not found");
            }

            var plugin = _plugins[source];
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
            if (_plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            if (!_plugins.ContainsKey(source))
            {
                throw new Exception("Source not found");
            }

            var plugin = _plugins[source];

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
            if (_plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            if (!_plugins.ContainsKey(source))
            {
                throw new Exception("Source not found");
            }

            var plugin = _plugins[source];

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
            if (_plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            if (!_plugins.ContainsKey(source))
            {
                throw new Exception("Source not found");
            }

            var plugin = _plugins[source];

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
