using Microsoft.AspNetCore.Mvc.Rendering;
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

        Task<int> CountTasksAsync(int projectId);

        Task DeleteProjectAsync(Project project);

        Task<ProjectMember?> GetMemberAsync(int projectId, string userId);

        Task<int> CountOwnersAsync(int projectId);

        Task AddMemberAsync(ProjectMember member);

        Task UpdateMemberAsync(ProjectMember member);

        Task RemoveMemberAsync(ProjectMember member);

        Task<IEnumerable<SelectListItem>> GetComboMembersAsync(int projectId);

        Task<IEnumerable<SelectListItem>> GetComboProjectsForUserAsync(string userId);
    }
}
