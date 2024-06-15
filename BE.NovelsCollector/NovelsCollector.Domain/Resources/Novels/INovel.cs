using NovelsCollector.Domain.Resources.Authors;
using NovelsCollector.Domain.Resources.Categories;
using NovelsCollector.Domain.Resources.Chapters;

namespace NovelsCollector.Domain.Resources.Novels
{
    public interface INovel : IResource
    {
        public string? Cover { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? Year { get; set; }
        public EnumStatus? Status { get; set; }
        public float? MaxRating { get; set; }
        public float? Rating { get; set; }
        public Author[]? Authors { get; set; }
        public Category[]? Categories { get; set; }
        public Chapter[]? Chapters { get; set; }
    }
}
