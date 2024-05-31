namespace NovelsCollector.SDK.Models
{
    public class Author
    {
        public string? Slug { get; set; }
        public string? Source { get; set; }
        public string? Name { get; set; }
        public Novel[]? Novels { get; set; }
    }
}
