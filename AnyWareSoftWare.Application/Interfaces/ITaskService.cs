using System.Collections.Generic;
using System.Threading.Tasks;
using AnyWareSoftWare.Application.DTOs;

namespace AnyWareSoftWare.Application.Interfaces
{
    public interface ITaskService
    {
        Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, int userId);
        Task<TaskDto?> GetTaskByIdAsync(int id, int userId);
        Task<IEnumerable<TaskDto>> GetAllMyTasksAsync(int userId);
        Task<TaskDto?> UpdateTaskStatusAsync(int id, UpdateTaskStatusDto dto, int userId);
    }
}