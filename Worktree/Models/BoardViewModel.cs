using Microsoft.AspNetCore.Mvc.Rendering;
using Worktree.Data.Entities;

namespace Worktree.Models
{
    public class BoardViewModel
    {
        public Project Project { get; set; } = null!;

        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        // current filters
        public string? AssigneeId { get; set; }

        public int? LabelId { get; set; }

        public TaskItemPrio? Priority { get; set; }

        // filter dropdowns
        public IEnumerable<SelectListItem> Members { get; set; } = new List<SelectListItem>();

        public IEnumerable<SelectListItem> Labels { get; set; } = new List<SelectListItem>();

        public BoardHeaderViewModel Header { get; set; } = null!;
    }
}