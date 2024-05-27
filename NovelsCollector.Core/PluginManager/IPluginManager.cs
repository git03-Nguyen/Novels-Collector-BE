using NovelsCollector.SDK.Plugins;

namespace NovelsCollector.Core.PluginManager
{
    public interface IPluginManager
    {
        Dictionary<string, IPlugin> Plugins { get; }
        void ReloadPlugins();
    }
}
