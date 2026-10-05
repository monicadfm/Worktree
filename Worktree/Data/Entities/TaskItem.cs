using System.ComponentModel.DataAnnotations;

namespace Worktree.Data.Entities
{
    public class TaskItem : IEntity
    {
        public int Id { get; set; }

        // 1:N each belongs to one proj
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // number inside the proj
        public int Number { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;

        public TaskItemPrio Priority { get; set; } = TaskItemPrio.Medium;

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        // who's working on it
        public string? AssigneeId { get; set; }
        public User? Assignee { get; set; }

        // who created it
        public string? OwnerId { get; set; } = string.Empty;
        public User? Owner { get; set; } = null;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // 1:N task comments
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        // N:N label through TaskLabel
        public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
    }
}
