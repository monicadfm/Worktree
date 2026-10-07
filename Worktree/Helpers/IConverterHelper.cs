using Worktree.Data.Entities;
using Worktree.Models;

namespace Worktree.Helpers
{
    public interface IConverterHelper
    {
        Project ToProject(ProjectViewModel model, bool isNew);

        ProjectViewModel ToProjectViewModel(Project project);

        TaskItem ToTaskItem(TaskViewModel model, bool isNew);

        TaskViewModel ToTaskViewModel(TaskItem task);
    }
}
