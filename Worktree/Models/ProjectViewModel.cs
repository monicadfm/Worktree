using System.ComponentModel.DataAnnotations;

namespace Worktree.Models
{
    public class ProjectViewModel
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
    }
}
