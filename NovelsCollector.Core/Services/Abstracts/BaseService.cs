﻿using MongoDB.Driver;
using NovelsCollector.Core.Models.Abstracts;

namespace NovelsCollector.Core.Services.Abstracts
{
    public class BaseService<T>
        where T : IBaseEntity, new()
    {
        private readonly IMongoCollection<T> _collection;

        public BaseService(MyMongoRepository myMongoRepository)
        {
            string className = typeof(T).Name;
            _collection = myMongoRepository.mongoDatabase.GetCollection<T>(className);
        }

        protected async Task<List<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        protected async Task<T> GetOneAsync(string id)
        {
            return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        protected async Task<T> GetOneByFieldAsync(string field, string value)
        {
            var filter = Builders<T>.Filter.Eq(field, value);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        protected async Task InsertOneAsync(T document)
        {
            await _collection.InsertOneAsync(document);
        }

        protected async Task UpdateOneAsync(string id, T document)
        {
            await _collection.ReplaceOneAsync(x => x.Id == id, document);
        }

        protected async Task UpdateOneByFieldAsync(string field, string value, T document)
        {
            var filter = Builders<T>.Filter.Eq(field, value);
            await _collection.ReplaceOneAsync(filter, document);
        }

        protected async Task DeleteOneAsync(string id)
        {
            await _collection.DeleteOneAsync(x => x.Id == id);
        }

        protected async Task DeleteOneByFieldAsync(string field, string value)
        {
            var filter = Builders<T>.Filter.Eq(field, value);
            await _collection.DeleteOneAsync(filter);
        }
    }
}
