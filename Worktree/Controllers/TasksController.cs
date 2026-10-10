using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Worktree.Data;
using Worktree.Data.Entities;
using Worktree.Helpers;
using Worktree.Models;

namespace Worktree.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserHelper _userHelper;
        private readonly IConverterHelper _converterHelper;
        private readonly ILabelRepository _labelRepository;

        public TasksController(
            ITaskRepository taskRepository,
            IProjectRepository projectRepository,
            IUserHelper userHelper,
            IConverterHelper converterHelper,
            ILabelRepository labelRepository)
        {
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _userHelper = userHelper;
            _converterHelper = converterHelper;
            _labelRepository = labelRepository;
        }

        // GET
        public async Task<IActionResult> Index(int? projectId)
        {
            if (projectId == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(projectId.Value);
            if (project == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            ViewBag.Project = project;
            return View(_taskRepository.GetAllForProject(project.Id));
        }

        // GET
        public async Task<IActionResult> Board(int? projectId, string? assigneeId, int? labelId, TaskItemPrio? priority)
        {
            if (projectId == null)
            {
                var defaultId = await GetDefaultProjectIdAsync();
                if (defaultId == null)
                {
                    return RedirectToAction("Index", "Projects");
                }

                return RedirectToAction(nameof(Board), new { projectId = defaultId });
            }

            var project = await _projectRepository.GetByIdAsync(projectId.Value);
            if (project == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var tasks = _taskRepository.GetAllForProject(project.Id);

            if (assigneeId == "none")
            {
                tasks = tasks.Where(t => t.AssigneeId == null);
            }
            else if (!string.IsNullOrEmpty(assigneeId))
            {
                tasks = tasks.Where(t => t.AssigneeId == assigneeId);
            }

            if (labelId.HasValue)
            {
                tasks = tasks.Where(t => t.TaskLabels.Any(tl => tl.LabelId == labelId.Value));
            }

            if (priority.HasValue)
            {
                tasks = tasks.Where(t => t.Priority == priority.Value);
            }

            var members = (await _projectRepository.GetComboMembersAsync(project.Id)).Skip(1).ToList();
            members.Insert(0, new SelectListItem { Text = "Unassigned", Value = "none" });
            members.Insert(0, new SelectListItem { Text = "All assignees", Value = "" });

            var labels = await _labelRepository.GetAllForProject(project.Id)
                .Select(l => new SelectListItem { Text = l.Name, Value = l.Id.ToString() })
                .ToListAsync();
            labels.Insert(0, new SelectListItem { Text = "All labels", Value = "" });

            var model = new BoardViewModel
            {
                Project = project,
                Tasks = await tasks.ToListAsync(),
                AssigneeId = assigneeId,
                LabelId = labelId,
                Priority = priority,
                Members = members,
                Labels = labels,
                Header = await _projectRepository.GetBoardHeaderAsync(project, user.Id, "Tasks")
            };

            return View(model);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAdd(int projectId, string? title)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            if (project.IsArchived)
            {
                return RedirectToAction(nameof(Board), new { projectId });
            }

            title = title?.Trim();
            if (string.IsNullOrEmpty(title) || title.Length < 3 || title.Length > 120)
            {
                TempData["QuickAddError"] = "The Title must be between 3 and 120 characters.";
                return RedirectToAction(nameof(Board), new { projectId });
            }

            var task = new TaskItem
            {
                ProjectId = project.Id,
                Title = title,
                Status = TaskItemStatus.ToDo,
                Priority = TaskItemPrio.Medium,
                CreatedById = user.Id
            };

            await _taskRepository.CreateTaskAsync(task);
            return RedirectToAction(nameof(Board), new { projectId });
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(int id, TaskItemStatus status, string? returnUrl)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(task.ProjectId);
            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (project == null || user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            if (!project.IsArchived && Enum.IsDefined(typeof(TaskItemStatus), status))
            {
                task.Status = status;
                task.UpdatedAt = DateTime.UtcNow;
                await _taskRepository.UpdateAsync(task);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Board), new { projectId = task.ProjectId });
        }

        // GET
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var task = await _taskRepository.GetByIdWithDetailsAsync(id.Value);
            if (task == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(task.ProjectId, user.Id))
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            ViewBag.CanDelete = task.CreatedById == user.Id
                || await _projectRepository.IsOwnerAsync(task.ProjectId, user.Id);

            ViewBag.CurrentUserId = user.Id;

            return View(task);
        }

        // GET
        public async Task<IActionResult> Create(int? projectId)
        {
            if (projectId == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(projectId.Value);
            if (project == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            if (project.IsArchived)
            {
                return RedirectToAction(nameof(Index), new { projectId = project.Id });
            }

            var model = new TaskViewModel
            {
                ProjectId = project.Id,
                ProjectKey = project.Key
            };
            await FillListsAsync(model, project.Id);

            return View(model);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskViewModel model)
        {
            var project = await _projectRepository.GetByIdAsync(model.ProjectId);
            if (project == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            if (project.IsArchived)
            {
                return RedirectToAction(nameof(Index), new { projectId = project.Id });
            }

            if (!string.IsNullOrEmpty(model.AssigneeId)
                && !await _projectRepository.IsMemberAsync(project.Id, model.AssigneeId))
            {
                ModelState.AddModelError(nameof(model.AssigneeId), "The assignee must be a member of the project.");
            }

            if (model.DueDate.HasValue && model.DueDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.DueDate), "The due date can't be in the past.");
            }

            if (ModelState.IsValid)
            {
                var task = _converterHelper.ToTaskItem(model, true);
                task.CreatedById = user.Id;

                var validLabelIds = await _labelRepository.GetAllForProject(project.Id)
                    .Select(l => l.Id)
                    .ToListAsync();

                foreach (var labelId in model.SelectedLabelIds.Where(validLabelIds.Contains).Distinct())
                {
                    task.TaskLabels.Add(new TaskLabel { LabelId = labelId });
                }

                await _taskRepository.CreateTaskAsync(task);
                return RedirectToAction(nameof(Details), new { id = task.Id });
            }

            model.ProjectKey = project.Key;
            await FillListsAsync(model, project.Id);
            return View(model);
        }

        // GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var task = await _taskRepository.GetByIdWithDetailsAsync(id.Value);
            if (task == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(task.ProjectId, user.Id))
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            if (task.Project.IsArchived)
            {
                return RedirectToAction(nameof(Details), new { id = task.Id });
            }

            var model = _converterHelper.ToTaskViewModel(task);
            model.ProjectKey = task.Project.Key;
            await FillListsAsync(model, task.ProjectId);

            ViewBag.Number = task.Number;
            return View(model);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaskViewModel model)
        {
            var task = await _taskRepository.GetByIdWithDetailsAsync(model.Id);
            if (task == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(task.ProjectId, user.Id))
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            if (task.Project.IsArchived)
            {
                return RedirectToAction(nameof(Details), new { id = task.Id });
            }

            if (!string.IsNullOrEmpty(model.AssigneeId)
                && !await _projectRepository.IsMemberAsync(task.ProjectId, model.AssigneeId))
            {
                ModelState.AddModelError(nameof(model.AssigneeId), "The assignee must be a member of the project.");
            }

            var dueDateChanged = model.DueDate?.Date != task.DueDate?.Date;
            if (dueDateChanged && model.DueDate.HasValue && model.DueDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.DueDate), "The due date can't be in the past.");
            }

            if (ModelState.IsValid)
            {
                task.Title = model.Title;
                task.Description = model.Description;
                task.Status = model.Status;
                task.Priority = model.Priority;
                task.DueDate = model.DueDate;
                task.AssigneeId = model.AssigneeId;
                task.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _taskRepository.UpdateAsync(task);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _taskRepository.ExistAsync(task.Id))
                    {
                        return new NotFoundViewResult("TaskNotFound");
                    }
                    else
                    {
                        throw;
                    }
                }

                var validLabelIds = await _labelRepository.GetAllForProject(task.ProjectId)
                    .Select(l => l.Id)
                    .ToListAsync();

                await _taskRepository.SetLabelsAsync(task,
                    model.SelectedLabelIds.Where(validLabelIds.Contains).Distinct().ToList());

                return RedirectToAction(nameof(Details), new { id = task.Id });
            }

            model.ProjectId = task.ProjectId;
            model.ProjectKey = task.Project.Key;
            await FillListsAsync(model, task.ProjectId);
            ViewBag.Number = task.Number;
            return View(model);
        }

        // GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var task = await _taskRepository.GetByIdWithDetailsAsync(id.Value);
            if (task == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(task.ProjectId, user.Id))
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            if (task.CreatedById != user.Id && !await _projectRepository.IsOwnerAsync(task.ProjectId, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (task.Project.IsArchived)
            {
                return RedirectToAction(nameof(Details), new { id = task.Id });
            }

            return View(task);
        }

        // POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _taskRepository.GetByIdWithDetailsAsync(id);
            if (task == null)
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(task.ProjectId, user.Id))
            {
                return new NotFoundViewResult("TaskNotFound");
            }

            if (task.CreatedById != user.Id && !await _projectRepository.IsOwnerAsync(task.ProjectId, user.Id))
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (task.Project.IsArchived)
            {
                return RedirectToAction(nameof(Details), new { id = task.Id });
            }

            var projectId = task.ProjectId;
            await _taskRepository.DeleteTaskAsync(task);

            return RedirectToAction(nameof(Index), new { projectId });
        }

        private async Task FillListsAsync(TaskViewModel model, int projectId)
        {
            model.Members = await _projectRepository.GetComboMembersAsync(projectId);
            model.AvailableLabels = await _labelRepository.GetAllForProject(projectId).ToListAsync();
        }

        private async Task<int?> GetDefaultProjectIdAsync()
        {
            var user = await _userHelper.GetUserWithProfileAsync(this.User.Identity!.Name!);
            if (user == null)
            {
                return null;
            }

            var defaultId = user.Profile?.DefaultProjectId;
            if (defaultId.HasValue && await _projectRepository.IsMemberAsync(defaultId.Value, user.Id))
            {
                return defaultId;
            }

            var first = await _projectRepository.GetAllForUser(user.Id).FirstOrDefaultAsync();

            return first?.Id;
        }
    }
}