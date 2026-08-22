using AnyWareSoftWare.Domain.Entities;

namespace AnyWareSoftWare.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseRepository<TaskItem> Tasks { get; }
        IBaseRepository<RefreshToken> RefreshTokens { get; }

        IBaseRepository<T> GetRepository<T>() where T : class;

        Task CompleteAsync();
    }
}
