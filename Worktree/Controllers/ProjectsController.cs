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

        public async Task<IActionResult> Index()
        {
            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(_projectRepository.GetAllForUser(user.Id));
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

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectViewModel model)
        {
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
        public async Task<IActionResult> ToggleArchive(int id)
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
    }
}
