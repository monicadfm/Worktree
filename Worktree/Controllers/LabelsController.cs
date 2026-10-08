using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Worktree.Data;
using Worktree.Helpers;
using Worktree.Models;

namespace Worktree.Controllers
{
    [Authorize]
    public class LabelsController : Controller
    {
        private readonly ILabelRepository _labelRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserHelper _userHelper;
        private readonly IConverterHelper _converterHelper;

        public LabelsController(
            ILabelRepository labelRepository,
            IProjectRepository projectRepository,
            IUserHelper userHelper,
            IConverterHelper converterHelper)
        {
            _labelRepository = labelRepository;
            _projectRepository = projectRepository;
            _userHelper = userHelper;
            _converterHelper = converterHelper;
        }

        // GET
        public async Task<IActionResult> Index(int? projectId)
        {
            if (projectId == null)
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(projectId.Value);
            if (project == null)
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            ViewBag.Project = project;
            ViewBag.IsOwner = await _projectRepository.IsOwnerAsync(project.Id, user.Id);
            return View(_labelRepository.GetAllForProject(project.Id));
        }

        // GET
        public async Task<IActionResult> Create(int? projectId)
        {
            if (projectId == null)
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(projectId.Value);
            if (project == null)
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(project.Id, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (project.IsArchived)
            {
                return RedirectToAction(nameof(Index), new { projectId = project.Id });
            }

            return View(new LabelViewModel
            {
                ProjectId = project.Id,
                ProjectKey = project.Key
            });
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LabelViewModel model)
        {
            var project = await _projectRepository.GetByIdAsync(model.ProjectId);
            if (project == null)
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(project.Id, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (project.IsArchived)
            {
                return RedirectToAction(nameof(Index), new { projectId = project.Id });
            }

            if (ModelState.IsValid)
            {
                if (await _labelRepository.NameExistsAsync(project.Id, model.Name.Trim()))
                {
                    ModelState.AddModelError(nameof(model.Name), "This project already has a label with this name.");
                }
                else
                {
                    var label = _converterHelper.ToLabel(model, true);
                    await _labelRepository.CreateAsync(label);
                    return RedirectToAction(nameof(Index), new { projectId = project.Id });
                }
            }

            model.ProjectKey = project.Key;
            return View(model);
        }

        // GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            var label = await _labelRepository.GetByIdAsync(id.Value);
            if (label == null)
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(label.ProjectId);
            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (project == null || user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(project.Id, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (project.IsArchived)
            {
                return RedirectToAction(nameof(Index), new { projectId = project.Id });
            }

            var model = _converterHelper.ToLabelViewModel(label);
            model.ProjectKey = project.Key;
            return View(model);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LabelViewModel model)
        {
            var label = await _labelRepository.GetByIdAsync(model.Id);
            if (label == null)
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(label.ProjectId);
            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (project == null || user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(project.Id, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (project.IsArchived)
            {
                return RedirectToAction(nameof(Index), new { projectId = project.Id });
            }

            if (ModelState.IsValid)
            {
                if (await _labelRepository.NameExistsAsync(project.Id, model.Name.Trim(), label.Id))
                {
                    ModelState.AddModelError(nameof(model.Name), "This project already has a label with this name.");
                }
                else
                {
                    label.Name = model.Name.Trim();
                    label.Color = model.Color.ToUpper();

                    try
                    {
                        await _labelRepository.UpdateAsync(label);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!await _labelRepository.ExistAsync(label.Id))
                        {
                            return new NotFoundViewResult("LabelNotFound");
                        }
                        else
                        {
                            throw;
                        }
                    }

                    return RedirectToAction(nameof(Index), new { projectId = project.Id });
                }
            }

            model.ProjectId = project.Id;
            model.ProjectKey = project.Key;
            return View(model);
        }

        // GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            var label = await _labelRepository.GetByIdAsync(id.Value);
            if (label == null)
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(label.ProjectId);
            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (project == null || user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(project.Id, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (project.IsArchived)
            {
                return RedirectToAction(nameof(Index), new { projectId = project.Id });
            }

            ViewBag.TaskCount = await _labelRepository.CountTasksAsync(label.Id);
            ViewBag.Project = project;
            return View(label);
        }

        // POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var label = await _labelRepository.GetByIdAsync(id);
            if (label == null)
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(label.ProjectId);
            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (project == null || user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("LabelNotFound");
            }

            if (!await _projectRepository.IsOwnerAsync(project.Id, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (project.IsArchived)
            {
                return RedirectToAction(nameof(Index), new { projectId = project.Id });
            }

            await _labelRepository.DeleteLabelAsync(label);
            return RedirectToAction(nameof(Index), new { projectId = project.Id });
        }
    }
}
