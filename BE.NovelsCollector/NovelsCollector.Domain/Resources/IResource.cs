namespace NovelsCollector.Domain.Resources
{
    public interface IResource
    {
        public int? Id { get; set; }
        public string? Slug { get; set; }
        public string? Source { get; set; }

    }
}
