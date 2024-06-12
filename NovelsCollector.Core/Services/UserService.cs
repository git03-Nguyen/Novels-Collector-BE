using NovelsCollector.Core.Models;
using NovelsCollector.Core.Services.Abstracts;

namespace NovelsCollector.Core.Services
{
    public class UserService : BaseService<User>
    {
        public UserService(MyMongoRepository myMongoRepository) : base(myMongoRepository)
        {
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await GetOneByFieldAsync("Email", email);
        }

        // Other specific methods for user service
    }
}
