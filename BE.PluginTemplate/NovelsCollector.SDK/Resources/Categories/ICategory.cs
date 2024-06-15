namespace NovelsCollector.Domain.Resources.Categories
{
    public interface ICategory : INovelsContainer
    {
        public string? Title { get; set; }
    }
}
