namespace Worktree.Data.Entities
{
    public class TaskLabel
    {
        // TaskItem and Label N:N
        // Primary key set in DataContext so no id needed
        public int TaskItemId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;

        public int LabelId { get; set; }
        public Label Label { get; set; } = null!;
    }
}
