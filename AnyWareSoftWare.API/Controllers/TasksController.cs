using System.Security.Claims;
using System.Threading.Tasks;
using AnyWareSoftWare.Application.DTOs;
using AnyWareSoftWare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnyWareSoftWare.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> CreateTask(CreateTaskDto dto)
        {
            var task = await _taskService.CreateTaskAsync(dto, GetUserId());
            return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id, GetUserId());
            if (task == null) return NotFound();
            return Ok(task);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMyTasks()
        {
            var tasks = await _taskService.GetAllMyTasksAsync(GetUserId());
            return Ok(tasks);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, UpdateTaskStatusDto dto)
        {
            var task = await _taskService.UpdateTaskStatusAsync(id, dto, GetUserId());
            if (task == null) return NotFound();
            return Ok(task);
        }
    }
}