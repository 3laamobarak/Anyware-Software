using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AnyWareSoftWare.Application.Interfaces;

namespace AnyWareSoftWare.Infrastructure.Services
{
    public class BackgroundQueue : IBackgroundQueue
    {
        private readonly Channel<int> _queue;

        public BackgroundQueue()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<int>(options);
        }

        public async Task<int> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }

        public void EnqueueTask(int taskId)
        {
            _queue.Writer.TryWrite(taskId);
        }
    }
}