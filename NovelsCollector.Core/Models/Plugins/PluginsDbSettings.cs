namespace NovelsCollector.Core.Models.Plugins
{
    public class PluginsDbSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string SourcesCollectionName { get; set; } = null!;
        public string ExportersCollectionName { get; set; } = null!;
    }
}
