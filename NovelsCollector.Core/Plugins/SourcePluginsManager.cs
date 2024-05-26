using NovelsCollector.SDK;
using NovelsCollector.SDK.SourcePlugins;
using System.Reflection;

namespace NovelsCollector.Core.Plugins
{
    public class SourcePluginsManager : ISourcePluginManager
    {
        private static readonly Dictionary<string, IPlugin> plugins = new Dictionary<string, IPlugin>();

        public Dictionary<string, IPlugin> Plugins => plugins;

        public SourcePluginsManager() 
        {
            ReloadPlugins();
        }

        public void AddPlugin(string pluginName, IPlugin plugin)
        {
            plugins.Add(pluginName, plugin);
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
                        IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                        plugins.Add(plugin.Name, plugin);
                    }
                }
            }
        }

        public async Task<string> ExecuteSearch(string? keyword, string? author, string? year)
        {
            string result = string.Empty;
            if (keyword != null)
            {
                foreach (var plugin in plugins)
                {
                    if (plugin.Value is ISourcePlugin sourcePlugin)
                    {
                        Console.WriteLine($"Searching in {plugin.Key}...");
                        result += await sourcePlugin.Search(keyword, author, year);
                    }
                }
            }
            return result;
        }
    }
}
