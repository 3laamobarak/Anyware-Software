using AnyWareSoftWare.Application.DTOs;
using AnyWareSoftWare.Application.Exceptions;
using AnyWareSoftWare.Application.Interfaces;
using AnyWareSoftWare.Domain.Entities;
using AnyWareSoftWare.Domain.Interfaces;

namespace AnyWareSoftWare.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRedisCacheService _cache;
        private readonly IBackgroundQueue _queue;

        public TaskService(IUnitOfWork unitOfWork, IRedisCacheService cache, IBackgroundQueue queue)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _queue = queue;
        }

        public async Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, int userId)
        {
            var today = DateTime.UtcNow.Date;

            var duplicate = await _unitOfWork.Tasks.GetByExpressionSingleAsync(
                t => t.UserId == userId && t.Title == dto.Title && t.CreatedAt.Date == today);

            if (duplicate != null)
                throw new ConflictException("Duplicate task title not allowed for the same user on the same day.");

            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                UserId = userId,
                Status = Domain.Enums.TaskStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Tasks.AddAsync(task);

            _queue.EnqueueTask(task.Id);

            return MapToDto(task);
        }

        public async Task<IEnumerable<TaskDto>> GetAllMyTasksAsync(int userId)
        {
            var tasks = await _unitOfWork.Tasks.GetByExpressionAsync(t => t.UserId == userId);

            return tasks
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .Select(MapToDto);
        }

        public async Task<TaskDto?> GetTaskByIdAsync(int id, int userId)
        {
            var cacheKey = $"task_{id}";
            var cached = await _cache.GetAsync<TaskDto>(cacheKey);

            if (cached != null)
            {
                if (cached.UserId != userId) return null;
                return cached;
            }

            var task = await _unitOfWork.Tasks.GetByExpressionSingleAsync(
                t => t.Id == id && t.UserId == userId);
            if (task == null) return null;

            var dto = MapToDto(task);
            await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));

            return dto;
        }

        public async Task<TaskDto?> UpdateTaskStatusAsync(int id, UpdateTaskStatusDto dto, int userId)
        {
            var task = await _unitOfWork.Tasks.GetByExpressionSingleAsync(
                t => t.Id == id && t.UserId == userId);
            if (task == null) return null;

            task.Status = dto.Status;
            await _unitOfWork.Tasks.UpdateAsync(task);

            await _cache.RemoveAsync($"task_{id}");

            return MapToDto(task);
        }

        private static TaskDto MapToDto(TaskItem task) => new()
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            CreatedAt = task.CreatedAt,
            UserId = task.UserId
        };
    }
}
