namespace NovelsCollector.Core.Models.Novels
{
    public class Chapter
    {
        public int Number { get; set; }
        public int NovelId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public Plugins.Source[] Sources { get; set; }
    }
}
