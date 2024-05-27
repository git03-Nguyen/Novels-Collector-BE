namespace NovelsCollector.SDK.Models
{
    public class Chapter
    {
        public string? Slug { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string[]? Sources { get; set; }

        // TODO: Identifier = { Source, Slug }, then each Chapter can have multiple identifiers
    }
}
