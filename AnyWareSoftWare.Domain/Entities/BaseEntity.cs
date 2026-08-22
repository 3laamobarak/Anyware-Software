using System.ComponentModel.DataAnnotations;

namespace AnyWareSoftWare.Domain.Entities
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
