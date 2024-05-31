using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.Reflection;

namespace NovelsCollector.Core.Services.Plugins.Sources
{
    public class SourcePluginsManager : ISourcePluginManager
    {
        private static readonly Dictionary<string, ISourcePlugin> plugins = new Dictionary<string, ISourcePlugin>();

        private static readonly string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        public Dictionary<string, ISourcePlugin> Plugins => plugins;

        #region Management
        public SourcePluginsManager() => ReloadPlugins();

        public void ReloadPlugins()
        {
            plugins.Clear();
            
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }
            string[] files = Directory.GetFiles(pluginsPath, "*.dll");

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

        public async Task AddPluginAsync(IFormFile file)
        {
            if (file == null)
            {
                throw new Exception("No file uploaded");
            }

            string path = Path.Combine(pluginsPath, file.FileName);

            // if exists, throw exception
            if (File.Exists(path))
            {
                throw new Exception("Plugin already exists");
            }

            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Assembly assembly = Assembly.LoadFile(path);
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

        public async Task RemovePlugin(string name)
        {
            if (!plugins.ContainsKey(name))
            {
                throw new Exception("Plugin not found");
            }

            plugins.Remove(name);
        }
        #endregion

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
            int totalPage = 0;

            var (novels, total) = await plugin.CrawlSearch(keyword: keyword, page: page);
            if (novels == null)
            {
                throw new Exception("No result found");
            }

            totalPage = total;
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
            var result = await plugin.CrawlDetail(novelSlug);
            if (result == null)
            {
                throw new Exception("No result found");
            }

            return result;
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
            var result = await plugin.CrawlChapter(novelSlug, chapterSlug);
            if (result == null)
            {
                throw new Exception("No result found");
            }

            return result;
        }

    }
}
