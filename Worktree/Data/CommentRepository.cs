using Microsoft.EntityFrameworkCore;
using Worktree.Data.Entities;

namespace Worktree.Data
{
    public class CommentRepository : GenericRepository<Comment>, ICommentRepository
    {
        private readonly DataContext _context;

        public CommentRepository(DataContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Comment?> GetByIdWithTaskAsync(int id)
        {
            return await _context.Comments
                .Include(c => c.TaskItem)
                .ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}