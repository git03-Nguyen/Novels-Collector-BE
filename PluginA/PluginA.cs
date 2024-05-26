using NovelsCollector.SDK;

namespace PluginA
{
    public class PluginA : IPlugin
    {
        public string Name => "PluginA";

        public string ExecuteCommand()
        {
            return "PluginA executed";
        }
    }
}
