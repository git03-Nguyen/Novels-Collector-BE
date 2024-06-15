using NovelsCollector.Domain.Helpers;

namespace NovelsCollector.Domain.Entities.Plugins
{
    public interface IPlugin : IEntity
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? Author { get; set; }
        public string? Assembly { get; set; }
        public string? Icon { get; set; }
        public bool? IsLoaded { get; set; }
        public IPluginFeature? PluginInstance { get; set; }
        public PluginLoadContext? LoadContext { get; set; }

    }
}
