using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Worktree.Data.Entities;
using Worktree.Models;

namespace Worktree.Data
{
    public class ProjectRepository : GenericRepository<Project>, IProjectRepository
    {
        private readonly DataContext _context;

        public ProjectRepository(DataContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddMemberAsync(ProjectMember member)
        {
            await _context.ProjectMembers.AddAsync(member);
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountOwnersAsync(int projectId)
        {
            return await _context.ProjectMembers
                .CountAsync(m => m.ProjectId == projectId && m.Role == UserRoles.Owner);
        }

        public async Task<BoardHeaderViewModel> GetBoardHeaderAsync(Project project, string userId, string activeTab)
        {
            var myProjects = await _context.ProjectMembers
                .Include(m => m.Project)
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.Project.IsArchived)
                .ThenBy(m => m.Project.Name)
                .ToListAsync();

            var defaultProjectId = await _context.UserProfiles
                .Where(p => p.UserId == userId)
                .Select(p => p.DefaultProjectId)
                .FirstOrDefaultAsync();

            return new BoardHeaderViewModel
            {
                Project = project,
                Role = myProjects.First(m => m.ProjectId == project.Id).Role,
                DefaultProjectId = defaultProjectId,
                MyProjects = myProjects,
                TaskCount = await _context.Tasks.CountAsync(t => t.ProjectId == project.Id),
                LabelCount = await _context.Labels.CountAsync(l => l.ProjectId == project.Id),
                MemberCount = await _context.ProjectMembers.CountAsync(m => m.ProjectId == project.Id),
                ActiveTab = activeTab
            };
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

        public async Task<ProjectMember?> GetMemberAsync(int projectId, string userId)
        {
            return await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId);
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

        public async Task RemoveMemberAsync(ProjectMember member)
        {
            var tasks = await _context.Tasks
                .Where(t => t.ProjectId == member.ProjectId && t.AssigneeId == member.UserId)
                .ToListAsync();
            foreach (var task in tasks)
            {
                task.AssigneeId = null;
            }

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == member.UserId && p.DefaultProjectId == member.ProjectId);
            if (profile != null)
            {
                profile.DefaultProjectId = null;
            }

            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMemberAsync(ProjectMember member)
        {
            _context.ProjectMembers.Update(member);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SelectListItem>> GetComboMembersAsync(int projectId)
        {
            var list = await _context.ProjectMembers
                .Where(m => m.ProjectId == projectId)
                .OrderBy(m => m.User.FirstName)
                .Select(m => new SelectListItem
                {
                    Text = m.User.FirstName + " " + m.User.LastName,
                    Value = m.UserId
                })
                .ToListAsync();

            list.Insert(0, new SelectListItem
            {
                Text = "(Unassigned)",
                Value = ""
            });

            return list;
        }

        public async Task<IEnumerable<SelectListItem>> GetComboProjectsForUserAsync(string userId)
        {
            var list = await _context.Projects
                .Where(p => !p.IsArchived && p.Members.Any(m => m.UserId == userId))
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Text = p.Key + " - " + p.Name,
                    Value = p.Id.ToString()
                })
                .ToListAsync();

            list.Insert(0, new SelectListItem
            {
                Text = "(None)",
                Value = ""
            });

            return list;
        }
    }
}
