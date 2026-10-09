using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Worktree.Data;
using Worktree.Helpers;
using Worktree.Models;

namespace Worktree.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserHelper _userHelper;
        private readonly IProjectRepository _projectRepository;
        private readonly ITaskRepository _taskRepository;

        public HomeController(
            ILogger<HomeController> logger,
            IUserHelper userHelper,
            IProjectRepository projectRepository,
            ITaskRepository taskRepository)
        {
            _logger = logger;
            _userHelper = userHelper;
            _projectRepository = projectRepository;
            _taskRepository = taskRepository;
        }

        public async Task<IActionResult> Index()
        {
            if (!this.User.Identity!.IsAuthenticated)
            {
                return View();
            }

            var user = await _userHelper.GetUserByEmailAsync(this.User.Identity.Name!);
            if (user == null)
            {
                return View();
            }

            var model = new DashboardViewModel
            {
                FirstName = user.FirstName,
                Projects = await _projectRepository.GetAllForUser(user.Id)
                    .Where(p => !p.IsArchived)
                    .ToListAsync(),
                AssignedToMe = await _taskRepository.GetAssignedToUser(user.Id).ToListAsync(),
                Overdue = await _taskRepository.GetOverdueForUser(user.Id).ToListAsync()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("error/404")]
        public IActionResult Error404()
        {
            return View();
        }
    }
}
