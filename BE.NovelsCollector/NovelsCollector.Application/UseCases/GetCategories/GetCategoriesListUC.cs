using NovelsCollector.Application.Exceptions;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Categories;

namespace NovelsCollector.Application.UseCases.GetCategories
{
    public class GetCategoriesListUC
    {
        private readonly IEnumerable<ISourcePlugin> _plugins;

        public GetCategoriesListUC(IEnumerable<ISourcePlugin> plugins) => _plugins = plugins;

        public async Task<Category[]> Execute(string source)
        {
            // Get the plugin in the Installed list
            var plugin = _plugins.FirstOrDefault(plugin => plugin.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, get the list of categories
            Category[]? categories = null;
            if (plugin.PluginInstance is ISourceFeature executablePlugin)
            {
                categories = await executablePlugin.CrawlCategories();
            }

            if (categories == null)
            {
                categories = Array.Empty<Category>();
            }

            return categories;
        }

    }
}
