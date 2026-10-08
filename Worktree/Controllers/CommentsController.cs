using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Worktree.Data;
using Worktree.Data.Entities;
using Worktree.Helpers;
using Worktree.Models;

namespace Worktree.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ICommentRepository _commentRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserHelper _userHelper;

        public CommentsController(
            ICommentRepository commentRepository,
            ITaskRepository taskRepository,
            IProjectRepository projectRepository,
            IUserHelper userHelper)
        {
            _commentRepository = commentRepository;
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _userHelper = userHelper;
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommentViewModel model)
        {
            var task = await _taskRepository.GetByIdAsync(model.TaskItemId);
            if (task == null)
            {
                return new NotFoundViewResult("CommentNotFound");
            }

            var project = await _projectRepository.GetByIdAsync(task.ProjectId);
            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (project == null || user == null || !await _projectRepository.IsMemberAsync(project.Id, user.Id))
            {
                return new NotFoundViewResult("CommentNotFound");
            }

            if (!project.IsArchived && ModelState.IsValid)
            {
                await _commentRepository.CreateAsync(new Comment
                {
                    TaskItemId = task.Id,
                    CreatedById = user.Id,
                    Body = model.Body.Trim(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            return RedirectToAction("Details", "Tasks", new { id = task.Id });
        }

        // GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new NotFoundViewResult("CommentNotFound");
            }

            var comment = await _commentRepository.GetByIdWithTaskAsync(id.Value);
            if (comment == null)
            {
                return new NotFoundViewResult("CommentNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(comment.TaskItem.ProjectId, user.Id))
            {
                return new NotFoundViewResult("CommentNotFound");
            }

            if (comment.CreatedById != user.Id)
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (comment.TaskItem.Project.IsArchived)
            {
                return RedirectToAction("Details", "Tasks", new { id = comment.TaskItemId });
            }

            ViewBag.TaskCode = $"{comment.TaskItem.Project.Key}-{comment.TaskItem.Number}";
            return View(new CommentViewModel
            {
                Id = comment.Id,
                TaskItemId = comment.TaskItemId,
                Body = comment.Body
            });
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CommentViewModel model)
        {
            var comment = await _commentRepository.GetByIdWithTaskAsync(model.Id);
            if (comment == null)
            {
                return new NotFoundViewResult("CommentNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(comment.TaskItem.ProjectId, user.Id))
            {
                return new NotFoundViewResult("CommentNotFound");
            }

            if (comment.CreatedById != user.Id)
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            if (comment.TaskItem.Project.IsArchived)
            {
                return RedirectToAction("Details", "Tasks", new { id = comment.TaskItemId });
            }

            if (ModelState.IsValid)
            {
                comment.Body = model.Body.Trim();
                comment.EditedAt = DateTime.UtcNow;
                await _commentRepository.UpdateAsync(comment);

                return RedirectToAction("Details", "Tasks", new { id = comment.TaskItemId });
            }

            model.TaskItemId = comment.TaskItemId;
            ViewBag.TaskCode = $"{comment.TaskItem.Project.Key}-{comment.TaskItem.Number}";
            return View(model);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _commentRepository.GetByIdWithTaskAsync(id);
            if (comment == null)
            {
                return new NotFoundViewResult("CommentNotFound");
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
            if (user == null || !await _projectRepository.IsMemberAsync(comment.TaskItem.ProjectId, user.Id))
            {
                return new NotFoundViewResult("CommentNotFound");
            }

            if (comment.CreatedById != user.Id)
            {
                return RedirectToAction("NotAuthorized", "Account");
            }

            var taskId = comment.TaskItemId;

            if (!comment.TaskItem.Project.IsArchived)
            {
                await _commentRepository.DeleteAsync(comment);
            }

            return RedirectToAction("Details", "Tasks", new { id = taskId });
        }
    }
}
