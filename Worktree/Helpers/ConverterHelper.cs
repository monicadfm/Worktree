using Worktree.Data.Entities;
using Worktree.Models;

namespace Worktree.Helpers
{
    public class ConverterHelper : IConverterHelper
    {
        public Project ToProject(ProjectViewModel model, bool isNew)
        {
            return new Project
            {
                Id = isNew ? 0 : model.Id,
                Name = model.Name,
                Key = model.Key,
                Description = model.Description
            };
        }

        public ProjectViewModel ToProjectViewModel(Project project)
        {
            return new ProjectViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Key = project.Key,
                Description = project.Description
            };
        }

        public TaskItem ToTaskItem(TaskViewModel model, bool isNew)
        {
            return new TaskItem
            {
                Id = isNew ? 0 : model.Id,
                ProjectId = model.ProjectId,
                Title = model.Title,
                Description = model.Description,
                Status = model.Status,
                Priority = model.Priority,
                DueDate = model.DueDate,
                AssigneeId = model.AssigneeId
            };
        }

        public TaskViewModel ToTaskViewModel(TaskItem task)
        {
            return new TaskViewModel
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                AssigneeId = task.AssigneeId
            };
        }
    }
}
