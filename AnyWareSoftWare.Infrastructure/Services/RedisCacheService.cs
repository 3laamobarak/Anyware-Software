using System;
using System.Text.Json;
using System.Threading.Tasks;
using AnyWareSoftWare.Application.Interfaces;
using StackExchange.Redis;

namespace AnyWareSoftWare.Infrastructure.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                if (!value.HasValue) return default;
                return JsonSerializer.Deserialize<T>(value.ToString());
            }
            catch (RedisException)
            {
                return default;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
            }
            catch (RedisException)
            {
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _db.StringSetAsync(key, json, expirationTime, false, When.Always);
            }
            catch (RedisException)
            {
            }
        }
    }
}
