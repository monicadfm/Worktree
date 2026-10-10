using Worktree.Data.Entities;

namespace Worktree.Models
{
    public class BoardHeaderViewModel
    {
        public Project Project { get; set; } = null!;

        public UserRoles Role { get; set; }

        public int? DefaultProjectId { get; set; }

        public List<ProjectMember> MyProjects { get; set; } = new List<ProjectMember>();

        public int TaskCount { get; set; }

        public int LabelCount { get; set; }

        public int MemberCount { get; set; }

        public string ActiveTab { get; set; } = string.Empty;
    }
}
