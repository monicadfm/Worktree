using System.ComponentModel.DataAnnotations;

namespace Worktree.Data.Entities
{
    public class Comment : IEntity
    {
        public int Id { get; set; }

        public int TaskItemId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;

        public string OwnerId { get; set; } = string.Empty;
        public User Owner { get; set; } = null!;

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Body { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EditeAt { get; set; }
    }
}
