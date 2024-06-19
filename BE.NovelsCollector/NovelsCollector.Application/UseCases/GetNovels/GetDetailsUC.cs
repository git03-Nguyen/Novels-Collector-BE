using NovelsCollector.Application.Exceptions;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Novels;

namespace NovelsCollector.Application.UseCases.GetNovels
{
    public class GetDetailsUC
    {
        private readonly IEnumerable<ISourcePlugin> _plugins;

        public GetDetailsUC(IEnumerable<ISourcePlugin> plugins) => _plugins = plugins;

        public async Task<Novel?> Execute(string source, string novelSlug)
        {
            // Get the plugin in the Installed list
            var plugin = _plugins.FirstOrDefault(plugin => plugin.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the novel detail
            Novel? novel = null;

            // Execute the plugin
            if (plugin.PluginInstance is ISourceFeature executablePlugin)
            {
                novel = await executablePlugin.CrawlDetail(novelSlug);
            }

            return novel ?? throw new NotFoundException("No result found");
        }
    }
}
