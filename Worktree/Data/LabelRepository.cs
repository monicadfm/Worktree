using Microsoft.EntityFrameworkCore;
using Worktree.Data.Entities;

namespace Worktree.Data
{
    public class LabelRepository : GenericRepository<Label>, ILabelRepository
    {
        private readonly DataContext _context;

        public LabelRepository(DataContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int> CountTasksAsync(int labelId)
        {
            return await _context.TaskLabels.CountAsync(tl => tl.LabelId == labelId);
        }

        public async Task DeleteLabelAsync(Label label)
        {
            var taskLabels = await _context.TaskLabels
                .Where(t1 => t1.LabelId == label.Id)
                .ToListAsync();
            _context.TaskLabels.RemoveRange(taskLabels);

            _context.Labels.Remove(label);
            await _context.SaveChangesAsync();
        }

        public IQueryable<Label> GetAllForProject(int projectId)
        {
            return _context.Labels
                .AsNoTracking()
                .Include(label => label.TaskLabels)
                .Where(label => label.ProjectId == projectId)
                .OrderBy(label => label.Name);
        }

        public async Task<bool> NameExistsAsync(int projectId, string name, int excludeLabelId = 0)
        {
            return await _context.Labels
                .AnyAsync(l => l.ProjectId == projectId && l.Name == name && l.Id != excludeLabelId);
        }
    }
}
