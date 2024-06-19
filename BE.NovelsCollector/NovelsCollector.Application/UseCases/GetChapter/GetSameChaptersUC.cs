using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Chapters;
using NovelsCollector.Domain.Resources.Novels;

namespace NovelsCollector.Application.UseCases.GetChapter
{
    public class GetSameChaptersUC
    {
        private readonly IEnumerable<ISourcePlugin> _plugins;

        public GetSameChaptersUC(IEnumerable<ISourcePlugin> plugins) => _plugins = plugins;

        public async Task<Dictionary<string, Chapter>?> Execute(Dictionary<string, Novel> novelInOtherSources, Chapter currentChapter)
        {
            // Check if the current chapter is null
            if (currentChapter.Source == null || currentChapter.NovelSlug == null || currentChapter.Number == null ||
                novelInOtherSources.Count == 0)
            {
                return null;
            }

            // Search for the chapter in other sources
            Dictionary<string, Chapter> chapters = new Dictionary<string, Chapter>();
            string thisSource = currentChapter.Source;
            int thisChapterNumber = currentChapter.Number.Value;

            // Using threads parallel to search for the chapter in other sources, each thread for each plugin
            var tasks = novelInOtherSources.Select(kvp => Task.Run(async () =>
            {
                var otherSource = kvp.Key;
                var otherNovel = kvp.Value;

                // Skip if the source is the same or the novel slug is null
                if (otherSource == thisSource || otherNovel.Slug == null) return;

                // Get the plugin in the Installed list
                var otherPlugin = _plugins.FirstOrDefault(plugin => plugin.Name == otherSource);
                if (otherPlugin == null) return;

                // Execute the plugin
                if (otherPlugin.PluginInstance is ISourceFeature executablePlugin)
                {
                    try
                    {
                        // Search for the chapter with the same number
                        var otherChapter = await executablePlugin.GetChapterAddrByNumber(otherNovel.Slug, otherNovel.Id, thisChapterNumber);
                        if (otherChapter != null)
                        {
                            otherChapter.Source = otherSource;
                            lock (chapters)
                            {
                                chapters.Add(otherSource, (Chapter)otherChapter);
                            }
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error when searching for chapters in {otherSource}: {ex.Message}");
                    }
                }
            })).ToArray();

            // Wait for all tasks to finish
            await Task.WhenAll(tasks);

            // Ensure to stop every tasks
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                {
                    Console.WriteLine($"Error when searching for chapters in other sources: {task.Exception?.InnerException?.Message}");
                }
            }

            // If no chapter is found, return null
            if (chapters.Count == 0) return null;

            return chapters.ToDictionary(chapter => chapter.Key, chapter => chapter.Value);
        }
    }
}
