using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Worktree.Data;
using Worktree.Data.Entities;
using Worktree.Helpers;
using Worktree.Models;

namespace Worktree.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserHelper _userHelper;
        private readonly IConverterHelper _converterHelper;

        public ProjectsController(IProjectRepository projectRepository, IUserHelper userHelper, IConverterHelper converterHelper)
        {
            _projectRepository = projectRepository;
            _userHelper = userHelper;
            _converterHelper = converterHelper;
        }

        public async Task<IActionResult> Index(bool showArchived)
        {
            var user = await _userHelper.GetUserWithProfileAsync(this.User.Identity!.Name!);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var projects = await _projectRepository.GetAllForUser(user.Id).ToListAsync();
            var active = projects.Where(p => !p.IsArchived).ToList();

            ViewBag.UserId = user.Id;
            ViewBag.DefaultProjectId = user.Profile?.DefaultProjectId;
            ViewBag.ShowArchived = showArchived;
            ViewBag.ActiveCount = active.Count;
            ViewBag.OwnedCount = active.Count(p => p.Members.Any(m => m.UserId == user.Id && m.Role == UserRoles.Owner));
            ViewBag.ArchivedCount = projects.Count - active.Count;

            return View(showArchived ? projects : active);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            var project = await _projectRepository.GetByIdWithMembersAsync(id.Value);
            if (project == null)
            { 
                return new NotFoundViewResult("ProjectNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            ViewBag.IsOwner = await _projectRepository.IsOwnerAsync(project.Id, user.Id);

            return View(project);
        }

        public IActionResult Create(string? returnTo)
        {
            ViewBag.ReturnTo = returnTo;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectViewModel model, string? returnTo)
        {
            ViewBag.ReturnTo = returnTo;
            model.Key = model.Key.ToUpper();
            ModelState.Remove(nameof(model.Key));
            TryValidateModel(model);

            if (ModelState.IsValid)
            {
                if (await _projectRepository.KeyExistsAsync(model.Key))
                {
                    ModelState.AddModelError(nameof(model.Key), "This key is already used by another project.");
                    return View(model);
                }

                var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var project = _converterHelper.ToProject(model, true);
                project.Members.Add(new ProjectMember
                {
                    UserId = user.Id,
                    Role = UserRoles.Owner
                });

                await _projectRepository.CreateAsync(project);

                if (returnTo == "board")
                {
                    return RedirectToAction("Board", "Tasks", new { projectId = project.Id });
                }

                return RedirectToAction(nameof(Details), new { id = project.Id });
            }

            return View(model);
        }

        // GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(id.Value);
            if (project == null)
            { 
                return new NotFoundViewResult("ProjectNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(project.Id, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            return View(_converterHelper.ToProjectViewModel(project));
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectViewModel model)
        {
            model.Key = model.Key.ToUpper();
            ModelState.Remove(nameof(model.Key));
            TryValidateModel(model);

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(model.Id, user.Id))
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(model.Id, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (ModelState.IsValid)
            {
                if (await _projectRepository.KeyExistsAsync(model.Key, model.Id))
                {
                    ModelState.AddModelError(nameof(model.Key), "This key is already used by another project.");
                    return View(model);
                }

                var project = await _projectRepository.GetByIdAsync(model.Id);
                if (project == null)
                {
                    return new NotFoundViewResult("ProjectNotFound");
                }

                project.Name = model.Name;
                project.Key = model.Key;
                project.Description = model.Description;

                try
                {
                    await _projectRepository.UpdateAsync(project);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _projectRepository.ExistAsync(model.Id))
                    {
                        return new NotFoundViewResult("ProjectNotFound");
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Details), new { id = model.Id });
            }

            return View(model);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleArchive(int id, string? returnTo)
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(project.Id, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            project.IsArchived = !project.IsArchived;
            await _projectRepository.UpdateAsync(project);

            if (returnTo == "members")
            {
                return RedirectToAction(nameof(Members), new { id = project.Id });
            }

            if (returnTo == "projects")
            {
                return RedirectToAction(nameof(Index), new { showArchived = project.IsArchived });
            }

            return RedirectToAction(nameof(Details), new { id = project.Id });
        }

        // GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(id.Value);
            if (project == null)
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(project.Id, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            ViewBag.TaskCount = await _projectRepository.CountTasksAsync(project.Id);
            return View(project);
        }

        // POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(project.Id, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            var taskCount = await _projectRepository.CountTasksAsync(project.Id);
            if (taskCount > 0)
            {
                ViewBag.TaskCount = taskCount;
                ViewBag.ErrorMessage = $"This project has {taskCount} task(s) and can't be deleted. Archive it instead.";
                return View(project);
            }

            try
            {
                await _projectRepository.DeleteProjectAsync(project);
            }
            catch (DbUpdateException)
            {
                ViewBag.TaskCount = taskCount;
                ViewBag.ErrorMessage = "The project couldn't be deleted because it's still in use.";
                return View(project);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET
        public async Task<IActionResult> Members(int? id)
        { 
            if (id == null)
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(id.Value, user.Id))
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            var model = await BuildMembersViewModelAsync(id.Value, user);
            if (model == null)
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            return View(model);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(ProjectMembersViewModel model)
        {
            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(model.ProjectId, user.Id))
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(model.ProjectId, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (ModelState.IsValid)
            {
                var newMember = await _userHelper.GetUserByEmailAsync(model.Email);
                if (newMember == null)
                {
                    ModelState.AddModelError(nameof(model.Email), "There's no registered user with this email.");
                }
                else if (await _projectRepository.IsMemberAsync(model.ProjectId, newMember.Id))
                {
                    ModelState.AddModelError(nameof(model.Email), "This user is already a member of the project.");
                }
                else 
                {
                    await _projectRepository.AddMemberAsync(new ProjectMember
                    {
                        ProjectId = model.ProjectId,
                        UserId = newMember.Id,
                        Role = model.Role
                    });

                    return RedirectToAction(nameof(Members), new { id = model.ProjectId });
                }
            }

            var page = await BuildMembersViewModelAsync(model.ProjectId, user);
            if (page == null)
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            page.Email = model.Email;
            page.Role = model.Role;
            return View("Members", page);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int projectId, string userId, UserRoles role)
        {
            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(projectId, user.Id))
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(projectId, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            var member = await _projectRepository.GetMemberAsync(projectId, userId);
            if (member == null)
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            if (member.Role == UserRoles.Owner && role == UserRoles.Member
                && await _projectRepository.CountOwnersAsync(projectId) == 1)
            {
                ModelState.AddModelError(string.Empty, "A project must have at least one Owner. Make someone else Owner first.");

                var page = await BuildMembersViewModelAsync(projectId, user);
                return page == null ? new NotFoundViewResult("ProjectNotFound") : View("Members", page);
            }

            member.Role = role;
            await _projectRepository.UpdateMemberAsync(member);

            return RedirectToAction(nameof(Members), new { id = projectId });

        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int projectId, string userId)
        {
            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(projectId, user.Id))
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            var isLeaving = userId == user.Id;

            // only Owners can remove other people; anyone can remove themselves (leave)
            if (!isLeaving && !await _projectRepository.IsOwnerAsync(projectId, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            var member = await _projectRepository.GetMemberAsync(projectId, userId);
            if (member == null)
            {
                return new NotFoundViewResult("ProjectNotFound");
            }

            if (member.Role == UserRoles.Owner && await _projectRepository.CountOwnersAsync(projectId) == 1)
            {
                ModelState.AddModelError(string.Empty, isLeaving
                    ? "You're the only Owner. Make someone else Owner before leaving."
                    : "The last Owner can't be removed.");

                var page = await BuildMembersViewModelAsync(projectId, user);
                return page == null ? new NotFoundViewResult("ProjectNotFound") : View("Members", page);
            }

            await _projectRepository.RemoveMemberAsync(member);

            if (isLeaving)
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Members), new { id = projectId });
        }

        private async Task<ProjectMembersViewModel?> BuildMembersViewModelAsync(int projectId, User user)
        {
            var project = await _projectRepository.GetByIdWithMembersAsync(projectId);
            if (project == null)
            {
                return null;
            }

            return new ProjectMembersViewModel
            {
                ProjectId = project.Id,
                Project = project,
                IsOwner = await _projectRepository.IsOwnerAsync(project.Id, user.Id),
                CurrentUserId = user.Id,
                Header = await _projectRepository.GetBoardHeaderAsync(project, user.Id, "Members")
            };
        }
    }
}
