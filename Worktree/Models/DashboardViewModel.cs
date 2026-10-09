using Worktree.Data.Entities;

namespace Worktree.Models
{
    public class DashboardViewModel
    {
        public string FirstName { get; set; } = string.Empty;

        public List<Project> Projects { get; set; } = new List<Project>();

        public List<TaskItem> AssignedToMe { get; set; } = new List<TaskItem>();

        public List<TaskItem> Overdue { get; set; } = new List<TaskItem>();
    }
}