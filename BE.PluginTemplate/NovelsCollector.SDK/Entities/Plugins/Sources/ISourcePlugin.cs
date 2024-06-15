namespace NovelsCollector.Domain.Entities.Plugins.Sources
{
    public interface ISourcePlugin : IPlugin
    {
        public string? Url { get; set; }
    }
}
