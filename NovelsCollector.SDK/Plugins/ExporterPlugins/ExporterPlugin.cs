namespace NovelsCollector.SDK.Plugins.ExporterPlugins
{
    public abstract class ExporterPlugin : BasePlugin
    {
        public string? FileFormat { get; set; }
        // Other specific properties for exporter plugins
    }
}
