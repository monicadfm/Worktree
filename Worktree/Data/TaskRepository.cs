using Microsoft.EntityFrameworkCore;
using Worktree.Data.Entities;

namespace Worktree.Data
{
    public class TaskRepository : GenericRepository<TaskItem>, ITaskRepository
    {
        private readonly DataContext _context;

        public TaskRepository(DataContext context) : base(context)
        {
            _context = context;
        }

        public IQueryable<TaskItem> GetAllForProject(int projectId)
        {
            return _context.Tasks
                .Include(t => t.Assignee)
                .Where(t => t.ProjectId == projectId)
                .OrderBy(t => t.Status)
                .ThenByDescending(t => t.Priority)
                .ThenBy(t => t.Number);
        }

        public async Task<TaskItem?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Include(t => t.CreatedBy)
                .Include(t => t.TaskLabels)
                .ThenInclude(tl => tl.Label)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task CreateTaskAsync(TaskItem task)
        {
            var project = await _context.Projects.FirstAsync(p => p.Id == task.ProjectId);

            task.Number = project.NextTaskNumber;
            project.NextTaskNumber++;

            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.Tasks.AddAsync(task);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTaskAsync(TaskItem task)
        {
            var comments = await _context.Comments
                .Where(c => c.TaskItemId == task.Id)
                .ToListAsync();
            _context.Comments.RemoveRange(comments);

            var taskLabels = await _context.TaskLabels
                .Where(tl => tl.TaskItemId == task.Id)
                .ToListAsync();
            _context.TaskLabels.RemoveRange(taskLabels);

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }
}