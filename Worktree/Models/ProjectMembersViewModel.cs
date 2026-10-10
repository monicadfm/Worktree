using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using Worktree.Data.Entities;

namespace Worktree.Models
{
    public class ProjectMembersViewModel
    {
        public int ProjectId { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "User email")]
        public string Email { get; set; } = string.Empty;

        public UserRoles Role { get; set; } = UserRoles.Member;

        // display only
        [ValidateNever]
        public Project Project { get; set; } = null!;

        public bool IsOwner { get; set; }

        [ValidateNever]
        public string CurrentUserId { get; set; } = string.Empty;

        [ValidateNever]
        public BoardHeaderViewModel Header { get; set; } = null!;
    }
}
