using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Worktree.Data;
using Worktree.Data.Entities;
using Worktree.Helpers;
using Worktree.Models;

namespace Worktree.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserHelper _userHelper;
        private readonly IMailHelper _mailHelper;
        private readonly IProjectRepository _projectRepository;

        public AccountController(IUserHelper userHelper, IMailHelper mailHelper, IProjectRepository projectRepository)
        {
            _userHelper = userHelper;
            _mailHelper = mailHelper;
            _projectRepository = projectRepository;
        }

        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userHelper.LoginAsync(model);
                if (result.Succeeded)
                {
                    if (this.Request.Query.Keys.Contains("ReturnUrl"))
                    {
                        return Redirect(this.Request.Query["ReturnUrl"].First()!);
                    }
                    return RedirectToAction("Index", "Home");
                }
            }

            this.ModelState.AddModelError(string.Empty, "Failed to login");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _userHelper.LogoutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterNewUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(model.Username);
                if (user == null)
                {
                    user = new User
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Username,
                        UserName = model.Username,
                        Profile = new UserProfile()
                    };

                    var result = await _userHelper.AddUserAsync(user, model.Password);
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        return View(model);
                    }

                    var loginViewModel = new LoginViewModel
                    {
                        Password = model.Password,
                        RememberMe = false,
                        Username = model.Username
                    };

                    var result2 = await _userHelper.LoginAsync(loginViewModel);
                    if (result2.Succeeded)
                    {
                        return RedirectToAction("Index", "Home");
                    }

                    ModelState.AddModelError(string.Empty, "The user couldn't be logged.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "This email is already registered.");
                }
            }

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> ChangeUser()
        {
            var user = await _userHelper.GetUserWithProfileAsync(this.User.Identity!.Name!);
            var model = new ChangeUserViewModel();

            if (user != null)
            {
                model.FirstName = user.FirstName;
                model.LastName = user.LastName;

                if (user.Profile != null)
                {
                    model.JobTitle = user.Profile.JobTitle;
                    model.Bio = user.Profile.Bio;
                    model.AvatarUrl = user.Profile.AvatarUrl;
                    model.Theme = user.Profile.Theme;
                    model.DefaultProjectId = user.Profile.DefaultProjectId;
                }

                model.Projects = await _projectRepository.GetComboProjectsForUserAsync(user.Id);
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangeUser(ChangeUserViewModel model)
        {
            var user = await _userHelper.GetUserWithProfileAsync(this.User.Identity!.Name!);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // the default project must be one of the user's projects
            if (model.DefaultProjectId.HasValue
                && !await _projectRepository.IsMemberAsync(model.DefaultProjectId.Value, user.Id))
            {
                ModelState.AddModelError(nameof(model.DefaultProjectId), "You can only choose a project you're a member of.");
            }

            if (ModelState.IsValid)
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;

                if (user.Profile != null)
                {
                    user.Profile.JobTitle = model.JobTitle;
                    user.Profile.Bio = model.Bio;
                    user.Profile.AvatarUrl = model.AvatarUrl;
                    user.Profile.Theme = model.Theme;
                    user.Profile.DefaultProjectId = model.DefaultProjectId;
                }

                var response = await _userHelper.UpdateUserAsync(user);

                if (response.Succeeded)
                {
                    ViewBag.UserMessage = "User updated!";
                }
                else
                {
                    ModelState.AddModelError(string.Empty, response.Errors.FirstOrDefault()!.Description);
                }
            }

            model.Projects = await _projectRepository.GetComboProjectsForUserAsync(user.Id);
            return View(model);
        }

        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(this.User.Identity!.Name!);
                if (user != null)
                {
                    var result = await _userHelper.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("ChangeUser");
                    }
                    else
                    {
                        this.ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault()!.Description);
                    }
                }
                else
                {
                    this.ModelState.AddModelError(string.Empty, "User not found.");
                }
            }

            return this.View(model);
        }

        public IActionResult RecoverPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RecoverPassword(RecoverPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "The email doesn't correspond to a registered user.");
                    return View(model);
                }

                var myToken = await _userHelper.GeneratePasswordResetTokenAsync(user);

                var link = this.Url.Action(
                    "ResetPassword",
                    "Account",
                    new { token = myToken },
                    protocol: HttpContext.Request.Scheme);

                Response response = _mailHelper.SendEmail(model.Email, "Worktree Password Reset",
                    $"<h1>Worktree Password Reset</h1>" +
                    $"To reset the password click in this link:<br/><br/>" +
                    $"<a href=\"{link}\">Reset Password</a>");

                if (response.IsSuccess)
                { 
                    ViewBag.Message = "The instructions to recover your password have been sent to your email.";
                    return View();
                }

                ModelState.AddModelError(string.Empty, "The email couldn't be sent. Please try again later.");
            }

            return View(model);
        }

        public IActionResult ResetPassword(string token)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(model.Username);
                if (user != null)
                {
                    var result = await _userHelper.ResetPasswordAsync(user, model.Token, model.Password);
                    if (result.Succeeded)
                    {
                        ViewBag.Message = "Password reset successful.";
                        return View();
                    }

                    ViewBag.Message = "Error while resetting the password.";
                    return View(model);
                }

                ViewBag.Message = "User not found.";
            }

            return View(model);
        }

        public IActionResult NotAuthorized()
        {
            return View();
        }
    }
}
