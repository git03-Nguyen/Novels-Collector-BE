namespace NovelsCollector.Core.PluginsManager
{
    public interface IPluginManager
    {
        // Reload all plugins
        public void ReloadPlugins();

        // Add a plugin from a zip file
        public Task AddPluginAsync(IFormFile file);

        // Remove a plugin by name
        public Task RemovePlugin(string name);
    }
}
