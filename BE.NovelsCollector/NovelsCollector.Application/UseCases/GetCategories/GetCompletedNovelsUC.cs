using NovelsCollector.Application.Exceptions;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Novels;

namespace NovelsCollector.Application.UseCases.GetCategories
{
    public class GetCompletedNovelsUC
    {
        private readonly IEnumerable<ISourcePlugin> _plugins;

        public GetCompletedNovelsUC(IEnumerable<ISourcePlugin> plugins) => _plugins = plugins;

        public async Task<Tuple<Novel[], int>> Execute(string source, int page = 1)
        {
            Novel[]? novels = null;
            int totalPage = -1;

            // Get the plugin in the Installed list
            var plugin = _plugins.FirstOrDefault(plugin => plugin.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the completed novels
            if (plugin.PluginInstance is ISourceFeature executablePlugin)
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
