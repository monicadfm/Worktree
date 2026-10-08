using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Worktree.Models
{
    public class LabelViewModel
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        [Required]
        [StringLength(30, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use a hex color like #3B82F6.")]
        public string Color { get; set; } = "#64748B";

        // display only
        [ValidateNever]
        public string ProjectKey { get; set; } = string.Empty;
    }
}
