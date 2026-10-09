using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Worktree.Data.Entities;

namespace Worktree.Models
{
    public class ChangeUserViewModel
    {
        [Required]
        [MaxLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        // 1:1 from UserProfile
        [MaxLength(80)]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        [MaxLength(300)]
        [Url]
        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        public AppTheme Theme { get; set; }

        [Display(Name = "Default Project")]
        public int? DefaultProjectId { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
    }
}
