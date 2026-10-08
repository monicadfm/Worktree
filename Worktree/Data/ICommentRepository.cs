using Worktree.Data.Entities;

namespace Worktree.Data
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        Task<Comment?> GetByIdWithTaskAsync(int id);
    }
}