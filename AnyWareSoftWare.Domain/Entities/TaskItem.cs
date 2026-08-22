using AnyWareSoftWare.Domain.Enums;

namespace AnyWareSoftWare.Domain.Entities
{
    public class TaskItem : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Domain.Enums.TaskStatus Status { get; set; } = Domain.Enums.TaskStatus.Pending;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }
}
