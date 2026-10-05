using System.ComponentModel.DataAnnotations;

namespace Worktree.Data.Entities
{
    public enum UserRoles
    {
        Owner,
        Member
    }

    public enum TaskItemStatus
    {
        [Display(Name = "To do")] ToDo,
        [Display(Name = "In progress")] InProgress,
        [Display(Name = "In review")] InReview,
        Done
    }

    public enum TaskItemPrio
    { 
        Low,
        Medium,
        High,
        Urgent
    }

    public enum AppTheme
    { 
        Light,
        Dark
    }
}
