using MongoDB.Driver;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Plugins.ExporterPlugins;
using System.Reflection;

namespace NovelsCollector.Core.Services.Plugins
{
    public class ExporterPluginsManager
    {
        // A dictionary to store the plugins
        private static readonly Dictionary<string, ExporterPlugin> _plugins = new Dictionary<string, ExporterPlugin>();

        // The path to the plugins folder
        private static readonly string _pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        // The collection of plugins in the database
        private static IMongoCollection<ExporterPlugin> _pluginsCollection = null;

        public Dictionary<string, ExporterPlugin> Plugins => _plugins;

        public ExporterPluginsManager(MongoDbContext mongoDbContext)
        {
            _pluginsCollection = mongoDbContext.ExporterPlugins;
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
                if (typeof(ExporterPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var plugin = (ExporterPlugin)Activator.CreateInstance(type);
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
            var listOfEnabledPlugins = new List<string> { "PluginCrawlTruyenFull" };
            foreach (var plugin in listOfEnabledPlugins)
            {
                LoadPlugin(plugin);
            }
        }


    }
}
