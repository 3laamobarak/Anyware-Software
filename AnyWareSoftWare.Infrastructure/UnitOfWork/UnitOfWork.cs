using AnyWareSoftWare.Domain.Entities;
using AnyWareSoftWare.Domain.Interfaces;
using AnyWareSoftWare.Infrastructure.Data;
using AnyWareSoftWare.Infrastructure.Repositories;

namespace AnyWareSoftWare.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IBaseRepository<TaskItem> Tasks { get; }
        public IBaseRepository<RefreshToken> RefreshTokens { get; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Tasks = new BaseRepository<TaskItem>(_context);
            RefreshTokens = new BaseRepository<RefreshToken>(_context);
        }

        public IBaseRepository<T> GetRepository<T>() where T : class =>
            new BaseRepository<T>(_context);

        public async Task CompleteAsync() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
