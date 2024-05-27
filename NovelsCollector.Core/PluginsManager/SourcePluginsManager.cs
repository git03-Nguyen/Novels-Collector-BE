using NovelsCollector.SDK;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.SourcePlugins;
using System.Reflection;

namespace NovelsCollector.Core.PluginsManager
{
    public class SourcePluginsManager : ISourcePluginManager
    {
        private static readonly Dictionary<string, ISourcePlugin> plugins = new Dictionary<string, ISourcePlugin>();

        public Dictionary<string, ISourcePlugin> Plugins => plugins;

        public SourcePluginsManager()
        {
            ReloadPlugins();
        }

        public void ReloadPlugins()
        {
            Console.WriteLine("Loading plugins...");

            plugins.Clear();

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            Console.WriteLine(path);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string[] files = Directory.GetFiles(path, "*.dll");

            foreach (string file in files)
            {
                Assembly assembly = Assembly.LoadFile(file);
                Type[] types = assembly.GetTypes();

                foreach (Type type in types)
                {
                    if (type.GetInterface("IPlugin") != null)
                    {
                        ISourcePlugin plugin = (ISourcePlugin)Activator.CreateInstance(type);
                        plugins.Add(plugin.Name, plugin);
                    }
                }
            }
        }

        public async Task<Tuple<Novel[], int>> Search(string? keyword, string? author, string? year)
        {
            if (plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            var novels = new List<Novel>();
            int totalPage = 0;

            foreach (var plugin in plugins.Values)
            {
                var (novelsResult, totalPageResult) = await plugin.CrawlSearch(keyword, 1);
                novels.AddRange(novelsResult);
                totalPage += totalPageResult;
            }

            return new Tuple<Novel[], int>(novels.ToArray(), totalPage);
        }

        public async Task<Novel> GetNovelDetail(string novelSlug)
        {
            if (plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            foreach (var plugin in plugins.Values)
            {
                try
                {
                    return await plugin.CrawlDetail(novelSlug);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            throw new Exception("Novel not found");
        }

        public async Task<string> GetChapter(Novel novel, Chapter chapter)
        {
            //if (plugins.Count == 0)
            //{
            //    throw new Exception("No plugins loaded");
            //}

            //foreach (var plugin in plugins.Values)
            //{
            //    try
            //    {
            //        return await plugin.CrawlChapter(novel.Slug, chapter.Slug);
            //    }
            //    catch (Exception)
            //    {
            //        continue;
            //    }
            //}

            throw new Exception("Chapter not found");
        }

        public Task<string> GetChapter(string novelSlug, string chapterSlug)
        {
            throw new NotImplementedException();
        }
    }
}
