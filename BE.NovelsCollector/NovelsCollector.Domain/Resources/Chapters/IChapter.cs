namespace NovelsCollector.Domain.Resources.Chapters
{
    public interface IChapter : IResource
    {
        public string? NovelSlug { get; set; }
        public int? Number { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
    }
}
