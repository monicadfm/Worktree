namespace Worktree.Data.Entities
{
    public class ProjectMember
    {
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public UserRoles Role { get; set; } = UserRoles.Member;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
