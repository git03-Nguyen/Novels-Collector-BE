using NovelsCollector.Application.Exceptions;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Chapters;

namespace NovelsCollector.Application.UseCases.GetNovels
{
    public class GetChaptersListUC
    {
        private readonly IEnumerable<ISourcePlugin> _plugins;

        public GetChaptersListUC(IEnumerable<ISourcePlugin> plugins) => _plugins = plugins;

        public async Task<Chapter[]?> Execute(string source, string novelSlug, string novelId)
        {
            // Get the plugin in the Installed list
            var plugin = _plugins.FirstOrDefault(plugin => plugin.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the list of chapters
            Chapter[]? chapters = null;

            // Execute the plugin
            if (plugin.PluginInstance is ISourceFeature executablePlugin)
            {
                chapters = await executablePlugin.CrawlListChapters(novelSlug, novelId);
            }

            if (chapters == null)
            {
                throw new NotFoundException("No result found");
            }

            return chapters;
        }

    }
}
