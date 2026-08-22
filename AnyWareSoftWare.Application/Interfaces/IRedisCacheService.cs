using System.Threading.Tasks;

namespace AnyWareSoftWare.Application.Interfaces
{
    public interface IRedisCacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, System.TimeSpan? expirationTime = null);
        Task RemoveAsync(string key);
    }
}