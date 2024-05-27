using NovelsCollector.SDK.Plugins.SourcePlugins;

namespace NovelsCollector.SDK.Models
{
    public class Category
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public Novel[]? Novels { get; set; }
        public ISourcePlugin[]? Sources { get; set; }
    }
}
