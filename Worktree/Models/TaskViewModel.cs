using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Worktree.Data.Entities;

namespace Worktree.Models
{
    public class TaskViewModel
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;

        public TaskItemPrio Priority { get; set; } = TaskItemPrio.Medium;

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Assignee")]
        public string? AssigneeId { get; set; }

        // display only
        [ValidateNever]
        public string ProjectKey { get; set; } = string.Empty;

        [ValidateNever]
        public IEnumerable<SelectListItem> Members { get; set; } = new List<SelectListItem>();

        [Display(Name = "Labels")]
        public List<int> SelectedLabelIds { get; set; } = new List<int>();

        [ValidateNever]
        public IEnumerable<Label> AvailableLabels { get; set; } = new List<Label>();
    }
}