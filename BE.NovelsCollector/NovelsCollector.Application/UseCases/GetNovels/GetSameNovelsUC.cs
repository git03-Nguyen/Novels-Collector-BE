using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Novels;
using System.Collections.Concurrent;

namespace NovelsCollector.Application.UseCases.GetNovels
{
    public class GetSameNovelsUC
    {
        private readonly IEnumerable<ISourcePlugin> _plugins;

        public GetSameNovelsUC(IEnumerable<ISourcePlugin> plugins) => _plugins = plugins;

        public async Task<Dictionary<string, Novel>?> Execute(string excludedSource, Novel novel)
        {
            // Check if the novel is null
            if (novel == null || novel.Title == null || novel.Authors == null || novel.Authors[0]?.Name == null)
                return null;

            // Search for the novel in other sources
            var novels = new ConcurrentDictionary<string, Novel>();

            // Using threads parallel to search for the novel in other sources, each thread for each plugin
            var tasks = _plugins.Select(plugin => Task.Run(() => QuickSearchNovels(plugin, excludedSource, novel, novels))).ToArray();

            // Wait for all tasks to finish
            await Task.WhenAll(tasks);

            // Ensure to stop every tasks
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                {
                    Console.WriteLine($"Error when searching for novels in other sources: {task.Exception?.InnerException?.Message}");
                }
            }

            while (!novels.TryAdd(excludedSource, novel)) ;

            // If no novel is found, return null
            if (novels.Count == 0) return null;

            // Set null the no-needed properties
            var result = novels.ToDictionary(kvp => kvp.Key, kvp => new Novel
            {
                Id = kvp.Value.Id,
                Title = kvp.Value.Title,
                Slug = kvp.Value.Slug,
            });

            // Return the dictionary
            return result;
        }

        // Method to handle the search logic for a single plugin
        private async Task QuickSearchNovels(ISourcePlugin plugin, string excludedSource, Novel novel, ConcurrentDictionary<string, Novel> novels)
        {
            if (plugin.Name == excludedSource) return;

            if (plugin.PluginInstance is ISourceFeature executablePlugin)
            {
                try
                {
                    // Step 1: Search by title
                    var (searchResults, _) = await executablePlugin.CrawlQuickSearch(novel.Title, 1);
                    if (searchResults == null) return;

                    // Step 2: Choose the novel with the same title and author
                    var trimmedTitle = novel.Title.Trim();
                    var sameNovel = searchResults.FirstOrDefault(n => (n.Title.Trim() == trimmedTitle));
                    if (sameNovel != null)
                    {
                        while (!novels.TryAdd(plugin.Name, (Novel)sameNovel)) ;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error when searching for novels in {plugin.Name}: {ex.Message}");
                }
            }
        }
    }
}
