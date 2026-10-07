using Microsoft.EntityFrameworkCore;
using Worktree.Data.Entities;

namespace Worktree.Data
{
    public class ProjectRepository : GenericRepository<Project>, IProjectRepository
    {
        private readonly DataContext _context;

        public ProjectRepository(DataContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int> CountTasksAsync(int projectId)
        {
            return await _context.Tasks.CountAsync(t => t.ProjectId == projectId);
        }

        public async Task DeleteProjectAsync(Project project)
        {
            var members = await _context.ProjectMembers
                .Where(m => m.ProjectId == project.Id)
                .ToListAsync();
            _context.ProjectMembers.RemoveRange(members);

            var labels = await _context.Labels
                .Where(l => l.ProjectId == project.Id)
                .ToListAsync();
            _context.Labels.RemoveRange(labels);

            var profiles = await _context.UserProfiles
                .Where(p => p.DefaultProjectId == project.Id)
                .ToListAsync();
            foreach (var profile in profiles)
            {
                profile.DefaultProjectId = null;
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        public IQueryable<Project> GetAllForUser(string userId)
        {
            return _context.Projects
                .Include(p => p.Members)
                .Include(p => p.Tasks)
                .Where(p => p.Members.Any(m => m.UserId == userId))
                .OrderBy(p => p.IsArchived)
                .ThenBy(p => p.Name);
        }

        public async Task<Project?> GetByIdWithMembersAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> IsMemberAsync(int projectId, string userId)
        {
            return await _context.ProjectMembers
                .AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);
        }

        public async Task<bool> IsOwnerAsync(int projectId, string userId)
        {
            return await _context.ProjectMembers
                .AnyAsync(m => m.ProjectId == projectId && m.UserId == userId && m.Role == UserRoles.Owner);
        }

        public async Task<bool> KeyExistsAsync(string key, int excludeProjectId = 0)
        {
            return await _context.Projects
                .AnyAsync(p => p.Key == key && p.Id != excludeProjectId);
        }
    }
}
