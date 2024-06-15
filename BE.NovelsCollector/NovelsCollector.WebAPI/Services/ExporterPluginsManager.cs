using NovelsCollector.Application.Exceptions;
using NovelsCollector.Application.Repositories;
using NovelsCollector.Application.UseCases;
using NovelsCollector.Domain.Entities.Plugins.Exporters;
using NovelsCollector.Domain.Resources.Novels;
using NovelsCollector.Infrastructure.Persistence.Entities;

namespace NovelsCollector.Core.Services
{
    public class ExporterPluginsManager : BasePluginsManager<ExporterPlugin, IExporterFeature>
    {
        private readonly ILogger<ExporterPluginsManager> _logger;
        private const string _pluginsFolderName = "Exporters";

        public ExporterPluginsManager(IPluginRepository<ExporterPlugin> myMongoRepository, ILogger<ExporterPluginsManager> logger)
            : base(myMongoRepository, _pluginsFolderName)
        {
            _logger = logger;
        }

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
            if (plugin.PluginInstance is IExporterFeature executablePlugin)
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
