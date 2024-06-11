using MongoDB.Driver;
using NovelsCollector.Core.Exceptions;
using NovelsCollector.Core.Models;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.ExporterPlugins;
using System.Runtime.CompilerServices;

namespace NovelsCollector.Core.Services
{
    public class ExporterPluginsManager
    {
        private readonly ILogger<ExporterPluginsManager> _logger;

        // Storing the plugins and their own contexts
        public List<ExporterPlugin> Installed { get; }

        // The path to the /source-plugins and /temp folders
        private readonly string _pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exporter-plugins");
        private readonly string _tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");

        // The collection of source-plugins in the database
        private IMongoCollection<ExporterPlugin> _pluginsCollection;

        // FOR DEBUGGING: The list of weak references to the unloaded contexts in the past
        public List<WeakReference> unloadedHistory = new List<WeakReference>();

        public ExporterPluginsManager(ILogger<ExporterPluginsManager> logger, MongoDbContext mongoDbContext)
        {
            _logger = logger;
            _pluginsCollection = mongoDbContext.ExporterPlugins;

            // Get installed plugins from the database
            Installed = _pluginsCollection.Find(plugin => true).ToList();

            // Create the /exporter-plugins and /temp folders if not exist
            if (!Directory.Exists(_pluginsPath)) Directory.CreateDirectory(_pluginsPath);
            if (!Directory.Exists(_tempPath)) Directory.CreateDirectory(_tempPath);

            // Load all installed plugins
            //loadAll();
        }


        // ------------------- EXPORTERs MANAGEMENT -------------------
        [MethodImpl(MethodImplOptions.NoInlining)]

        // Export the novel using the plugin: Export(Novel novel, string pluginName), return a file stream
        public async Task<string?> Export(string pluginName, Novel novel, Stream outputStream)
        {
            // Find the plugin by name
            var plugin = Installed.Find(plugin => plugin.Name == pluginName);

            // If the plugin is not found or not loaded, return null
            if (plugin == null)
                throw new NotFoundException("Exporter plugin not found");
            if (plugin.PluginInstance == null)
                throw new BadHttpRequestException("Exporter plugin not loaded");

            // If the plugin is loaded, call the Export method
            if (plugin.PluginInstance is IExporterPlugin executablePlugin)
            {
                await executablePlugin.Export(novel, outputStream);
                return plugin.Extension;
            }
            else
            {
                return null;
            }

        }


    }
}
