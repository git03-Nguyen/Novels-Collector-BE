using NovelsCollector.Domain.Resources.Novels;

namespace NovelsCollector.Domain.Resources.Categories
{
    public class Category : ICategory
    {
        public int? Id { get; set; } = null;
        public string? Source { get; set; } = null;
        public string? Slug { get; set; } = null;
        public string? Title { get; set; } = null;
        public Novel[]? Novels { get; set; } = null;
    }
}
