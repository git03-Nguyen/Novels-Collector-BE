using Amazon.Runtime.Internal.Transform;
using MongoDB.Driver;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.ExporterPlugins;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NovelsCollector.Core.Services.Plugins
{
    public class ExporterPluginsManager
    {
        private readonly ILogger<SourcePluginsManager> _logger;

        // 2 dictionaries to store the plugins and their own contexts
        public Dictionary<string, ExporterPlugin> Plugins { get; } = new Dictionary<string, ExporterPlugin>();
        private Dictionary<string, PluginLoadContext> _pluginLoadContexts = new Dictionary<string, PluginLoadContext>();

        // The path to the plugins folder
        private readonly string _pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exporter-plugins");

        // TODO: The collection of source-plugins in the database
        private IMongoCollection<ExporterPlugin> _pluginsCollection = null;

        // FOR TESTING: The list of installed plugins
        private List<string> _enabledPlugins = new List<string>
        {
            
        };

        // FOR DEBUGGING: The list of weak references to the unloaded contexts
        public List<WeakReference> unloadedContexts = new List<WeakReference>();

        public ExporterPluginsManager(ILogger<SourcePluginsManager> logger, MongoDbContext mongoDbContext)
        {
            _logger = logger;
            _pluginsCollection = mongoDbContext.ExporterPlugins;

            if (!Directory.Exists(_pluginsPath))
            {
                Directory.CreateDirectory(_pluginsPath);
            }

            // Load all installed plugins
            // ReloadPlugins();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UnloadAll()
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
        public void ReloadPlugins()
        {
            UnloadAll();
            foreach (var plugin in _enabledPlugins)
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
            string? pathToDll = Directory.GetFiles(pluginPath, $"Exporter.{pluginName}.dll").FirstOrDefault();
            if (pathToDll == null)
            {
                _logger.LogError($"\tPlugin \"Exporter.{pluginName}.dll\" not found");
                return false;
            }

            // Create a new context to load the plugin into
            _logger.LogInformation($"\tLOADING {pluginName} from /exporter-plugins/{pluginName}");
            PluginLoadContext loadContext = new PluginLoadContext(pathToDll);

            // Load the plugin assembly ("Source.{pluginName}.dll")
            Assembly pluginAssembly = loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(pathToDll));
            Type[] types = pluginAssembly.GetTypes();
            var hasSourcePlugin = false;
            foreach (var type in types)
            {
                if (typeof(ExporterPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var plugin = Activator.CreateInstance(type) as ExporterPlugin;
                    if (plugin == null) continue;
                    // Add the plugin (ExporterPlugin) to the dictionary => each assembly must have only 1 ExporterPlugin
                    Plugins.Add(pluginName, plugin);
                    hasSourcePlugin = true;
                    break;
                }
            }

            // If there is no plugin SourcePlugin loaded, cancel the loading process
            if (!hasSourcePlugin)
            {
                _logger.LogError($"\tNo ExporterPlugin found in Exporter.{pluginName}.dll");
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

        // Export the novel using the plugin: Export(Novel novel, string pluginName), return a file stream
        public async Task<(Stream?, string?)> Export(Novel novel, string pluginName)
        {
            if (!Plugins.ContainsKey(pluginName))
            {
                _logger.LogError($"\tPlugin {pluginName} not found");
                return (null, null);
            }

            var plugin = Plugins[pluginName];
            if (plugin is IExporterPlugin executablePlugin)
            {
                var file = await executablePlugin.Export(novel);
                return (file, plugin.FileFormat);
            }

            return (null, null);
        }


    }
}
