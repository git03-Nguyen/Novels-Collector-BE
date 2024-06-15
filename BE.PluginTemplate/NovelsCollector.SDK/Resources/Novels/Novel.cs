using NovelsCollector.Domain.Resources.Authors;
using NovelsCollector.Domain.Resources.Categories;
using NovelsCollector.Domain.Resources.Chapters;

namespace NovelsCollector.Domain.Resources.Novels
{
    public class Novel : INovel
    {
        public int? Id { get; set; } = null;
        public string? Slug { get; set; } = null;
        public string? Source { get; set; } = null;
        public string? Cover { get; set; } = null;
        public string? Title { get; set; } = null;
        public string? Description { get; set; }
        public int? Year { get; set; } = null;
        public EnumStatus? Status { get; set; } = null;
        public float? MaxRating { get; set; } = null;
        public float? Rating { get; set; } = null;
        public Author[]? Authors { get; set; } = null;
        public Category[]? Categories { get; set; } = null;
        public Chapter[]? Chapters { get; set; } = null;
    }
}
