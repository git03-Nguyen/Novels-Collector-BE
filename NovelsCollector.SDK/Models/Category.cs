namespace NovelsCollector.SDK.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string? Slug { get; set; }
        public string? Name { get; set; }
        public Novel[]? Novels { get; set; }
        public string[]? Sources { get; set; }

        // TODO: Identifier = { Source, Slug }, then each Category can have multiple identifiers
    }
}
