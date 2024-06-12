using NovelsCollector.Core.Exceptions;
using NovelsCollector.Core.Models;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.ExporterPlugins;

namespace NovelsCollector.Core.Services
{
    public class ExporterPluginsManager : BasePluginsManager<ExporterPlugin, IExporterPlugin>
    {
        private const string pluginsFolderName = "exporter-plugins";
        private const string collectionName = "Exporters"; // TODO: move to a constant file

        public ExporterPluginsManager(ILogger<ExporterPluginsManager> logger, MongoDbContext mongoDbContext)
            : base(logger, mongoDbContext, collectionName, pluginsFolderName) { }

        // -------------- MANAGE FOR EXPORTER FEATURES --------------

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
