namespace NovelsCollector.Domain.Entities.Users
{
    public interface IRole : IEntity
    {
        public string? Name { get; set; }
    }
}
