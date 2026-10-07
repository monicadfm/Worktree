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
    }
}
