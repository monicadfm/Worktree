using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Worktree.Data.Entities
{
    public class UserProfile
    {
        [Key]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        [MaxLength(80)]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        [MaxLength(300)]
        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        public AppTheme Theme { get; set; } = AppTheme.Light;

        [Display(Name = "Default Project")]
        public int? DefaultProjectId { get; set; }
        public Project? DefaultProject { get; set; }
    }
}
