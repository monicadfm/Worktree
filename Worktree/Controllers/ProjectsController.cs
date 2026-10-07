using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    }
}
