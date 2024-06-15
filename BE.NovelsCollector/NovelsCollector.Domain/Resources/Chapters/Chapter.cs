namespace NovelsCollector.Domain.Resources.Chapters
{
    public class Chapter : IChapter
    {
        public int? Id { get; set; } = null;
        public string? Source { get; set; } = null;
        public string? NovelSlug { get; set; } = null;
        public string? Slug { get; set; } = null;
        public int? Number { get; set; } = null;
        public string? Title { get; set; } = null;
        public string? Content { get; set; } = null;
    }
}
