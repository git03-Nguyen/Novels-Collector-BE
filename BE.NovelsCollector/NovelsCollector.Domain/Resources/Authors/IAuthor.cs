namespace NovelsCollector.Domain.Resources.Authors
{
    public interface IAuthor : INovelsContainer
    {
        public string? Name { get; set; }
    }
}
