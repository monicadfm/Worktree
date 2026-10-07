using Worktree.Data.Entities;

namespace Worktree.Data
{
    public interface IProjectRepository : IGenericRepository<Project>
    {
        IQueryable<Project> GetAllForUser(string userId);

        Task<Project?> GetByIdWithMembersAsync(int id);

        Task<bool> IsMemberAsync(int projectId, string userId);

        Task<bool> IsOwnerAsync(int projectId, string userId);

        Task<bool> KeyExistsAsync(string key, int excludeProjectId = 0);
    }
}
