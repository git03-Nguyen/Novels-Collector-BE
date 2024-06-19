using NovelsCollector.Application.Exceptions;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Chapters;

namespace NovelsCollector.Application.UseCases.GetChapter
{
    public class GetChapterContentUC
    {
        private readonly IEnumerable<ISourcePlugin> _plugins;

        public GetChapterContentUC(IEnumerable<ISourcePlugin> plugins) => _plugins = plugins;

        public async Task<Chapter?> Execute(string source, string novelSlug, string chapterSlug)
        {
            // Get the plugin in the Installed list
            var plugin = _plugins.FirstOrDefault(plugin => plugin.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the chapter content
            Chapter? chapter = null;

            // Execute the plugin
            if (plugin.PluginInstance is ISourceFeature executablePlugin)
            {
                chapter = await executablePlugin.CrawlChapter(novelSlug, chapterSlug);
            }

            return (Chapter?)(chapter ?? throw new NotFoundException("No result found"));
        }
    }
}
