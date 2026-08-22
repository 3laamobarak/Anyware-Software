using System.Linq.Expressions;

namespace AnyWareSoftWare.Domain.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();

        Task<T?> GetByExpressionSingleAsync(
            Expression<Func<T, bool>> expression,
            Expression<Func<T, object>>[]? includes = null);

        Task<IEnumerable<T>> GetByExpressionAsync(
            Expression<Func<T, bool>> expression,
            Expression<Func<T, object>>[]? includes = null);

        Task<int> CountAsync(Expression<Func<T, bool>>? expression = default);

        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task HardDeleteAsync(T entity);

        IQueryable<T> GetTableNoTracking();
        IQueryable<T> GetTableAsTracking();
        Task SaveChangesAsync();
    }
}
