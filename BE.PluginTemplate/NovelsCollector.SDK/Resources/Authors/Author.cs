using NovelsCollector.Domain.Resources.Novels;

namespace NovelsCollector.Domain.Resources.Authors
{
    public class Author : IAuthor
    {
        public int? Id { get; set; } = null;
        public string? Slug { get; set; } = null;
        public string? Source { get; set; } = null;
        public string? Name { get; set; } = null;
        public Novel[]? Novels { get; set; } = null;
    }
}
