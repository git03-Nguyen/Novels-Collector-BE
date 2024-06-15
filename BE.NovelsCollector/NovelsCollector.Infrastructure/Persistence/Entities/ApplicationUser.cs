using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
using NovelsCollector.Domain.Entities;
using NovelsCollector.Domain.Entities.Users;

namespace NovelsCollector.Infrastructure.Persistence.Entities
{
    [CollectionName("Users")]
    public class ApplicationUser : MongoIdentityUser<Guid>, IIdentityEntity
    {
    }
}
