using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Worktree.Data.Entities;

namespace Worktree.Data
{
    public class DataContext : IdentityDbContext<User>
    {
        // one dbset = one table
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Label> Labels { get; set; }
        public DbSet<TaskLabel> TaskLabels { get; set; }

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        { 
        
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProjectMember>()
                .HasKey(pm => new { pm.ProjectId, pm.UserId });

            modelBuilder.Entity<TaskLabel>()
                .HasKey(t1 => new { t1.TaskItemId, t1.LabelId });

            modelBuilder.Entity<Project>()
                .HasIndex(p => p.Key)
                .IsUnique();

            modelBuilder.Entity<TaskItem>()
                .HasIndex(t => new { t.ProjectId, t.Number })
                .IsUnique();

            var cascadeFKS = modelBuilder.Model
                .GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

            foreach (var fk in cascadeFKS)
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
