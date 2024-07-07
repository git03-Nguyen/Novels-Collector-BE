using NovelsCollector.Application.Exceptions;
using NovelsCollector.Domain.Entities.Plugins.Exporters;
using NovelsCollector.Domain.Resources.Novels;

namespace NovelsCollector.Application.UseCases.ExportNovel
{
    public class ExportNovelUC
    {
        private readonly IEnumerable<IExporterPlugin> _plugins;
        public ExportNovelUC(IEnumerable<IExporterPlugin> plugins) => _plugins = plugins;


        // Export the novel using the plugin: Export(Novel novel, string pluginName), return a file stream
        public async Task<string?> Execute(string pluginName, Novel novel, Stream outputStream)
        {
            // Find the plugin by name
            var plugin = _plugins.FirstOrDefault(plugin => plugin.Name == pluginName);

            // If the plugin is not found or not loaded, return null
            if (plugin == null)
                throw new NotFoundException("Exporter plugin not found");
            if (plugin.PluginInstance == null)
                throw new BadRequestException("Exporter plugin not loaded");

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
