namespace NovelsCollector.Domain.Entities.Users
{
    internal interface IUser : IEntity
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public IRole? Role { get; set; }
    }
}
