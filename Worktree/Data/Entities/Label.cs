using System.ComponentModel.DataAnnotations;

namespace Worktree.Data.Entities
{
    public class Label : IEntity
    {
        public int Id { get; set; }

        // 1:N each belongs to one proj
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Required]
        [StringLength(30, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use a hex color like #3B82F6.")]
        public string Color { get; set; } = "#64748B";

        // N:N label through TaskLabel
        public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
    }
}
