using System.ComponentModel.DataAnnotations;

namespace Worktree.Data.Entities
{
    public class Project : IEntity
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(5, MinimumLength = 2)]
        [RegularExpression("^[A-Z]+$", ErrorMessage = "The key must be 2 to 5 uppercase letters.")]
        public string Key { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Archived")]
        public bool IsArchived { get; set; }

        public int NextTaskNumber { get; set; } = 1;

        // N:N with User through ProjectMember
        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();

        // 1:N allows many labels/tasks in a proj
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public ICollection<Label> Labels { get; set; } = new List<Label>();

    }
}
