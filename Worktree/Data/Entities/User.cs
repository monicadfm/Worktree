using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Worktree.Data.Entities
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";

        // 1:1 one profile for every user
        public UserProfile? Profile { get; set; }

        // N:N with Project through ProjectMember
        public ICollection<ProjectMember> Memberships { get; set; } = new List<ProjectMember>();

        // 1:N tasks assigned/created by X user and their comments
        public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
        public ICollection<TaskItem> ReportedTasks { get; set; } = new List<TaskItem>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
