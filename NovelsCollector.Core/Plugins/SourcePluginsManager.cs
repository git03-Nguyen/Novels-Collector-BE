using NovelsCollector.SDK;
using System.Reflection;

namespace NovelsCollector.Core.Plugins
{
    public class SourcePluginsManager : IPluginManager
    {
        private static readonly Dictionary<string, IPlugin> plugins = new Dictionary<string, IPlugin>();

        public Dictionary<string, IPlugin> Plugins => plugins;

        public SourcePluginsManager() 
        {
            LoadPlugins();
        }

        public void AddPlugin(string pluginName, IPlugin plugin)
        {
            plugins.Add(pluginName, plugin);
        }

        public string ExecutePlugin(string pluginName)
        {
            if (plugins.ContainsKey(pluginName))
            {
                return plugins[pluginName].ExecuteCommand();
            }
            return "Plugin not found";
        }

        public void LoadPlugins()
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
    }
}
