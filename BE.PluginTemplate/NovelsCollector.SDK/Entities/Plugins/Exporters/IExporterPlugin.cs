namespace NovelsCollector.Domain.Entities.Plugins.Exporters
{
    public interface IExporterPlugin : IPlugin
    {
        public string? Extension { get; set; }

    }
}
