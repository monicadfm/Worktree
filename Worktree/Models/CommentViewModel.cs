using System.ComponentModel.DataAnnotations;

namespace Worktree.Models
{
    public class CommentViewModel
    {
        public int Id { get; set; }

        public int TaskItemId { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        [Display(Name = "Comment")]
        public string Body { get; set; } = string.Empty;
    }
}