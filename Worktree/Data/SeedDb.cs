using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Worktree.Data.Entities;
using Worktree.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Worktree.Data
{
    public class SeedDb
    {
        private readonly DataContext _context;
        private readonly IUserHelper _userHelper;

        public SeedDb(DataContext context, IUserHelper userHelper)
        {
            _context = context;
            _userHelper = userHelper;
        }

        public async Task SeedAsync()
        {
            await _context.Database.MigrateAsync();

            var teste1 = await CheckUserAsync("Teste1", "Teste1", "teste1@worktree.com");
            var teste2 = await CheckUserAsync("Teste2", "Teste2", "teste2@worktree.com");

            if (!_context.Projects.Any())
            {
                // projects
                var web = AddProject("Website relaunch", "WEB", "New version of the company website.", teste1, teste2);
                var mob = AddProject("Mobile app", "MOB", "Companion app for iOS and Android.", teste2, teste1);

                // labels
                var frontend = AddLabel(web, "frontend", "#2457D6");
                var backend = AddLabel(web, "backend", "#6D28D9");
                var bug = AddLabel(web, "bug", "#B91C1C");
                var design = AddLabel(mob, "design", "#0F766E");

                // tasks
                AddTask(web, "Set up repository and solution", TaskItemStatus.Done, TaskItemPrio.Medium, teste1, teste1, -6, backend);
                AddTask(web, "Login and register pages", TaskItemStatus.InReview, TaskItemPrio.High, teste2, teste1, 1, frontend);
                AddTask(web, "Password reset by email", TaskItemStatus.InProgress, TaskItemPrio.Urgent, teste1, teste1, -1, backend);
                AddTask(web, "Project members page", TaskItemStatus.InProgress, TaskItemPrio.High, teste1, teste2, 2, backend, frontend);
                AddTask(web, "Deleting a project with tasks crashes", TaskItemStatus.ToDo, TaskItemPrio.Urgent, teste2, teste2, -2, bug);
                AddTask(web, "Write the README", TaskItemStatus.ToDo, TaskItemPrio.Low, null, teste1, 5);
                AddTask(mob, "App icon and splash screen", TaskItemStatus.ToDo, TaskItemPrio.Medium, teste2, teste2, 4, design);

                await _context.SaveChangesAsync();
            }
        }

        private async Task<User> CheckUserAsync(string firstName, string lastName, string email)
        {
            var user = await _userHelper.GetUserByEmailAsync(email);
            if (user == null)
            {
                user = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    UserName = email,
                    Profile = new UserProfile()
                };

                var result = await _userHelper.AddUserAsync(user, "Teste123!");
                if (result != IdentityResult.Success)
                {
                    throw new InvalidOperationException("Could not create the user in seeder");
                }
            }

            return user;
        }

        private Project AddProject(string name, string key, string description, User owner, params User[] members)
        {
            var project = new Project
            {
                Name = name,
                Key = key,
                Description = description
            };

            project.Members.Add(new ProjectMember { User = owner, Role = UserRoles.Owner });

            foreach (var member in members)
            {
                project.Members.Add(new ProjectMember { User = member, Role = UserRoles.Member });
            }

            _context.Projects.Add(project);
            return project;
        }

        private Label AddLabel(Project project, string name, string color)
        {
            var label = new Label
            {
                Project = project,
                Name = name,
                Color = color
            };

            _context.Labels.Add(label);
            return label;
        }

        private void AddTask(Project project, string title, TaskItemStatus status, TaskItemPrio priority, User? assignee, User createdBy, int dueInDays, params Label[] labels)
        {
            var task = new TaskItem
            {
                Project = project,
                Number = project.NextTaskNumber,
                Title = title,
                Status = status,
                Priority = priority,
                Assignee = assignee,
                CreatedBy = createdBy,
                DueDate = DateTime.Today.AddDays(dueInDays)
            };

            project.NextTaskNumber++;

            foreach (var label in labels)
            {
                task.TaskLabels.Add(new TaskLabel { Label = label });
            }

            _context.Tasks.Add(task);
        }
    }
}
