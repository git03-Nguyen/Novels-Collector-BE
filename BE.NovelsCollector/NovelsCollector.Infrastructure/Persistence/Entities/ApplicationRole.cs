﻿using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
using NovelsCollector.Domain.Entities.Users;

namespace NovelsCollector.Infrastructure.Persistence.Entities
{
    [CollectionName("Roles")]
    public class ApplicationRole : MongoIdentityRole<Guid>, IIdentityEntity
    { }
}
