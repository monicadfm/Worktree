using Worktree.Data.Entities;

namespace Worktree.Data
{
    public interface ILabelRepository : IGenericRepository<Label>
    {
        IQueryable<Label> GetAllForProject(int projectId);

        Task<bool> NameExistsAsync(int projectId, string name, int excludeLabelId = 0);

        Task<int> CountTasksAsync(int labelId);

        Task DeleteLabelAsync(Label label);
    }
}
