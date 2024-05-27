using NovelsCollector.SDK.Plugins.SourcePlugins;

namespace NovelsCollector.SDK.Models
{
    public class Chapter
    {
        public int Number { get; set; }
        public int NovelId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public ISourcePlugin Plugin { get; set; }
    }
}
