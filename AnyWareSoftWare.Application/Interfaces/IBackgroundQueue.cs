using System.Threading;
using System.Threading.Tasks;

namespace AnyWareSoftWare.Application.Interfaces
{
    public interface IBackgroundQueue
    {
        void EnqueueTask(int taskId);
        Task<int> DequeueAsync(CancellationToken cancellationToken);
    }
}