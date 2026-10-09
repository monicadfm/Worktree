using Worktree.Data.Entities;

namespace Worktree.Data
{
    public interface ITaskRepository : IGenericRepository<TaskItem>
    {
        IQueryable<TaskItem> GetAllForProject(int projectId);

        Task<TaskItem?> GetByIdWithDetailsAsync(int id);

        Task CreateTaskAsync(TaskItem task);

        Task DeleteTaskAsync(TaskItem task);

        Task SetLabelsAsync(TaskItem task, IEnumerable<int> labelIds);

        IQueryable<TaskItem> GetAssignedToUser(string userId);

        IQueryable<TaskItem> GetOverdueForUser(string userId);
    }
}