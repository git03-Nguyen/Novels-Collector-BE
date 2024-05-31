using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.Reflection;

namespace NovelsCollector.Core.Services.Plugins
{
    public class SourcePluginsManager
    {
        private static readonly Dictionary<string, SourcePlugin> plugins = new Dictionary<string, SourcePlugin>();

        private static readonly string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        public Dictionary<string, SourcePlugin> Plugins => plugins;

        public SourcePluginsManager() => Reload();

        public void LoadPlugin(string pluginName)
        {
            if (plugins.ContainsKey(pluginName))
            {
                return;
            }

            string pluginPath = Path.Combine(pluginsPath, $"{pluginName}");
            if (!Directory.Exists(pluginPath))
            {
                return;
            }

            //var dlls = Directory.GetFiles(pluginPath, "*.dll");
            var dll = Directory.GetFiles(pluginPath, "*.dll").FirstOrDefault();

            if (!File.Exists(dll))
            {
                return;
            }

            Assembly assembly = Assembly.LoadFile(dll);
            Type[] types = assembly.GetTypes();

            foreach (var type in types)
            {
                if (typeof(SourcePlugin).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var plugin = (SourcePlugin)Activator.CreateInstance(type);
                    plugins.Add(pluginName, plugin);
                }
            }
        }

        public void Reload()
        {
            plugins.Clear();

            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }

            // For test:
            var listOfEnabledPlugins = new List<string> { "PluginCrawlTruyenFull" };
            foreach (var plugin in listOfEnabledPlugins)
            {
                LoadPlugin(plugin);
            }
        }

        public async Task<Tuple<Novel[]?, int>> Search(string source, string keyword, string? author, string? year, int page = 1)
        {
            if (plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            if (!plugins.ContainsKey(source))
            {
                throw new Exception("Source not found");
            }

            var plugin = plugins[source];
            Novel[]? novels = null;
            int totalPage = -1;

            if (plugin is ISourcePlugin executablePlugin)
            {
                (novels, totalPage) = await executablePlugin.CrawlSearch(keyword: keyword, page: page);
            }

            if (novels == null)
            {
                throw new Exception("No result found");
            }

            return new Tuple<Novel[]?, int>(novels, totalPage);
        }

        public async Task<Novel?> GetNovelDetail(string source, string novelSlug)
        {
            if (plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            if (!plugins.ContainsKey(source))
            {
                throw new Exception("Source not found");
            }

            var plugin = plugins[source];

            Novel? novel = null;

            if (plugin is ISourcePlugin executablePlugin)
            {
                novel = await executablePlugin.CrawlDetail(novelSlug);
            }

            if (novel == null)
            {
                throw new Exception("No result found");
            }

            return novel;
        }

        public async Task<Chapter?> GetChapter(string source, string novelSlug, string chapterSlug)
        {
            if (plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            if (!plugins.ContainsKey(source))
            {
                throw new Exception("Source not found");
            }

            var plugin = plugins[source];

            Chapter? chapter = null;
            if (plugin is ISourcePlugin executablePlugin)
            {
                chapter = await executablePlugin.CrawlChapter(novelSlug, chapterSlug);
            }

            if (chapter == null)
            {
                throw new Exception("No result found");
            }

            return chapter;
        }

    }
}
