namespace NovelsCollector.SDK.Models
{
    public enum EnumStatus
    {
        ComingSoon = 0,
        OnGoing = 1,
        Completed = 2,
        Drop = 3,
    }

    public class Novel
    {
        public int Id { get; set; }
        public string? Slug { get; set; }
        public string? Source { get; set; }
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
