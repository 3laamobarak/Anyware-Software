using System.Linq.Expressions;
using AnyWareSoftWare.Domain.Entities;
using AnyWareSoftWare.Domain.Interfaces;
using AnyWareSoftWare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnyWareSoftWare.Infrastructure.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly AppDbContext _context;

        public BaseRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public virtual async Task<T?> GetByIdAsync(int id) =>
            await _context.Set<T>().FindAsync(id);

        public virtual async Task<IEnumerable<T>> GetAllAsync() =>
            await _context.Set<T>().ToListAsync();

        public virtual async Task<T?> GetByExpressionSingleAsync(
            Expression<Func<T, bool>> expression,
            Expression<Func<T, object>>[]? includes = null)
        {
            var query = ApplyIncludes(_context.Set<T>(), includes);
            return await query.FirstOrDefaultAsync(expression);
        }

        public virtual async Task<IEnumerable<T>> GetByExpressionAsync(
            Expression<Func<T, bool>> expression,
            Expression<Func<T, object>>[]? includes = null)
        {
            var query = ApplyIncludes(_context.Set<T>(), includes);
            return await query.Where(expression).ToListAsync();
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? expression = default) =>
            expression is null
                ? await _context.Set<T>().CountAsync()
                : await _context.Set<T>().CountAsync(expression);

        public virtual async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task UpdateAsync(T entity)
        {
            if (entity is BaseEntity be) be.UpdatedAt = DateTime.UtcNow;
            _context.Set<T>().Update(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(T entity)
        {
            if (entity is BaseEntity be)
            {
                be.IsDeleted = true;
                _context.Set<T>().Update(entity);
            }
            else
            {
                _context.Set<T>().Remove(entity);
            }
            await _context.SaveChangesAsync();
        }

        public virtual async Task HardDeleteAsync(T entity)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }

        public IQueryable<T> GetTableNoTracking() => _context.Set<T>().AsNoTracking();

        public IQueryable<T> GetTableAsTracking() => _context.Set<T>();

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        private static IQueryable<T> ApplyIncludes(
            IQueryable<T> query, Expression<Func<T, object>>[]? includes)
        {
            if (includes != null && includes.Length > 0)
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            return query;
        }
    }
}
