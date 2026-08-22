using AnyWareSoftWare.Application.DTOs;
using AnyWareSoftWare.Application.Exceptions;
using AnyWareSoftWare.Application.Interfaces;
using AnyWareSoftWare.Application.Services;
using AnyWareSoftWare.Domain.Entities;
using AnyWareSoftWare.Domain.Enums;
using AnyWareSoftWare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AnyWareSoftWare.Tests
{
    public class TaskServiceTests
    {
        private static AppDbContext NewContext() =>
            new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static TaskService NewService(AppDbContext ctx, out Mock<IBackgroundQueue> queue)
        {
            var cache = new Mock<IRedisCacheService>();
            cache.Setup(c => c.GetAsync<TaskDto>(It.IsAny<string>())).ReturnsAsync((TaskDto?)null);
            queue = new Mock<IBackgroundQueue>();
            var uow = new Infrastructure.UnitOfWork.UnitOfWork(ctx);
            return new TaskService(uow, cache.Object, queue.Object);
        }

        [Fact]
        public async Task CreateTask_PersistsAndEnqueuesForProcessing()
        {
            using var ctx = NewContext();
            var service = NewService(ctx, out var queue);

            var dto = await service.CreateTaskAsync(
                new CreateTaskDto { Title = "Task A", Description = "d", Priority = TaskPriority.High }, userId: 1);

            Assert.Equal("Task A", dto.Title);
            Assert.Equal("Pending", dto.Status);
            Assert.Equal(1, await ctx.Tasks.CountAsync());
            queue.Verify(q => q.EnqueueTask(dto.Id), Times.Once);
        }

        [Fact]
        public async Task CreateTask_DuplicateTitleSameDaySameUser_ThrowsConflict()
        {
            using var ctx = NewContext();
            var service = NewService(ctx, out _);
            await service.CreateTaskAsync(new CreateTaskDto { Title = "Dup" }, userId: 1);

            await Assert.ThrowsAsync<ConflictException>(() =>
                service.CreateTaskAsync(new CreateTaskDto { Title = "Dup" }, userId: 1));
        }

        [Fact]
        public async Task CreateTask_SameTitleDifferentUser_IsAllowed()
        {
            using var ctx = NewContext();
            var service = NewService(ctx, out _);
            await service.CreateTaskAsync(new CreateTaskDto { Title = "Shared" }, userId: 1);

            var second = await service.CreateTaskAsync(new CreateTaskDto { Title = "Shared" }, userId: 2);

            Assert.NotNull(second);
            Assert.Equal(2, await ctx.Tasks.CountAsync());
        }

        [Fact]
        public async Task GetAllMyTasks_SortsByPriorityDescThenCreatedAt()
        {
            using var ctx = NewContext();
            ctx.Tasks.AddRange(
                new TaskItem { Title = "low", Priority = TaskPriority.Low, UserId = 1, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new TaskItem { Title = "high", Priority = TaskPriority.High, UserId = 1, CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
                new TaskItem { Title = "medium", Priority = TaskPriority.Medium, UserId = 1, CreatedAt = DateTime.UtcNow.AddMinutes(-3) });
            await ctx.SaveChangesAsync();
            var service = NewService(ctx, out _);

            var result = (await service.GetAllMyTasksAsync(1)).ToList();

            Assert.Equal(new[] { "high", "medium", "low" }, result.Select(t => t.Title).ToArray());
        }

        [Fact]
        public async Task GetTaskById_OtherUsersTask_ReturnsNull()
        {
            using var ctx = NewContext();
            ctx.Tasks.Add(new TaskItem { Id = 10, Title = "owned", UserId = 1 });
            await ctx.SaveChangesAsync();
            var service = NewService(ctx, out _);

            var result = await service.GetTaskByIdAsync(10, userId: 999);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTaskStatus_ChangesStatusAndInvalidatesCache()
        {
            using var ctx = NewContext();
            ctx.Tasks.Add(new TaskItem { Id = 5, Title = "t", UserId = 1, Status = Domain.Enums.TaskStatus.Pending });
            await ctx.SaveChangesAsync();
            var cache = new Mock<IRedisCacheService>();
            var uow = new Infrastructure.UnitOfWork.UnitOfWork(ctx);
            var service = new TaskService(uow, cache.Object, Mock.Of<IBackgroundQueue>());

            var result = await service.UpdateTaskStatusAsync(
                5, new UpdateTaskStatusDto { Status = Domain.Enums.TaskStatus.Done }, userId: 1);

            Assert.Equal("Done", result!.Status);
            cache.Verify(c => c.RemoveAsync("task_5"), Times.Once);
        }
    }
}
