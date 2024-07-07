using NovelsCollector.Application.Exceptions;
using NovelsCollector.Application.Utils;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Novels;

namespace NovelsCollector.Application.UseCases.GetNovels
{
    public class SearchNovelsUC
    {
        private readonly IEnumerable<ISourcePlugin> _plugins;

        public SearchNovelsUC(IEnumerable<ISourcePlugin> plugins) => _plugins = plugins;

        public async Task<Tuple<Novel[]?, int>> Execute(string source, string? keyword, string? title, string? author, int page = 1)
        {
            // Check if query is empty
            if (keyword == null && title == null && author == null)
                throw new BadRequestException("Query is empty");

            // Get the plugin in the Installed list
            var plugin = _plugins.FirstOrDefault(plugin => plugin.Name == source);

            // If the plugin is not found or not loaded, throw an exception
            if (plugin == null)
                throw new NotFoundException("Plugin not found");
            if (plugin.PluginInstance == null)
                throw new Exception("Plugin not loaded");

            // If the plugin is loaded, search for novels
            Novel[]? novels = null;
            int totalPage = -1;
            string query = keyword ?? author ?? title ?? "";

            // Execute the plugin
            if (plugin.PluginInstance is ISourceFeature executablePlugin)
            {
                // standardize the query
                var sQuery = RemoveVietnameseSigns.Convert(query.ToLower());
                (novels, totalPage) = await executablePlugin.CrawlSearch(sQuery, page);

                // if search by keyword, do nothing
                if (query == keyword) { }

                // filter if search by title
                else if (query == title)
                {
                    novels = novels?.Where(novel => RemoveVietnameseSigns.Convert(novel.Title).ToLower().Contains(sQuery)).ToArray();
                }
                // filter if search by author
                else
                {
                    novels = novels?.Where(novel => RemoveVietnameseSigns.Convert(novel.Authors[0]?.Name).ToLower().Contains(sQuery)).ToArray();
                }
            }

            if (novels == null) throw new NotFoundException("No result found");

            return new Tuple<Novel[]?, int>(novels, totalPage);
        }

    }
}
