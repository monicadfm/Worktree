using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
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

        public ICollection<ProjectMember> Memberships { get; set; } = new List<ProjectMember>();
    }
}
