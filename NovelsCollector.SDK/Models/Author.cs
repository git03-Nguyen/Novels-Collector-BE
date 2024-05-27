namespace NovelsCollector.SDK.Models
{
    public class Author
    {
        public string? Slug { get; set; }
        public string? Name { get; set; }
        public Novel[]? Novels { get; set; }
        public string[]? Sources { get; set; }

        // TODO: Identifier = { Source, Slug }, then each Author can have multiple identifiers
    }
}
