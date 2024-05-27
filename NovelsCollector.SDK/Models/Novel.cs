using NovelsCollector.SDK.Plugins.SourcePlugins;

namespace NovelsCollector.SDK.Models
{
    public class Novel
    {
        public int Id { get; set; }
        public string Cover { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Year { get; set; }
        public bool Status { get; set; }
        public float Rating { get; set; }
        public Author[] Authors { get; set; }
        public Category[] Categories { get; set; }
        public ISourcePlugin[] Plugins { get; set; }
    }
}
