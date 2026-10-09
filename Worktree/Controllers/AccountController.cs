using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
        {
            var providers = await _userHelper.GetExternalLoginsAsync();
            if (!providers.Any(p => p.Name == provider))
            {
                ModelState.AddModelError(string.Empty, "This login provider isn't configured on this server.");
                return View(nameof(Login));
            }

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl = SafeReturnUrl(returnUrl) });
            var properties = _userHelper.ConfigureExternalLogin(provider, redirectUrl!);
            return Challenge(properties, provider);
        }

        // GET
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from the external provider: {remoteError}");
                return View(nameof(Login));
            }

            var info = await _userHelper.GetExternalLoginInfoAsync();
            if (info == null)
            {
                if (this.User.Identity!.IsAuthenticated)
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Couldn't sign in with Google.");
                return View(nameof(Login));
            }

            // if alr linked to a normmal account
            var result = await _userHelper.ExternalLoginSignInAsync(info);
            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Google didn't return the required data (email).");
                return View(nameof(Login));
            }

            var user = await _userHelper.GetUserByEmailAsync(email);

            // create account from the Google profile
            if (user == null)
            {
                var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? email.Split('@')[0];
                var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
                var picture = info.Principal.FindFirstValue("urn:google:picture");

                if (firstName.Length > 50)
                {
                    firstName = firstName[..50];
                }

                if (lastName.Length > 50)
                {
                    lastName = lastName[..50];
                }

                if (picture?.Length > 300)
                {
                    picture = null;
                }

                user = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true,
                    Profile = new UserProfile
                    {
                        AvatarUrl = picture
                    }
                };

                var createResult = await _userHelper.AddUserAsync(user);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(nameof(Login));
                }
            }

            // link Google to the account
            var linkResult = await _userHelper.AddLoginAsync(user, info);
            if (!linkResult.Succeeded)
            {
                foreach (var error in linkResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(nameof(Login));
            }

            await _userHelper.SignInAsync(user);
            return RedirectToLocal(returnUrl);
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

        private string? SafeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                return null;
            }

            var path = returnUrl.Split('?')[0].Split('#')[0].TrimEnd('/');
            string[] authPages = { "/Account/ExternalLoginCallback", "/Account/ExternalLogin", "/Account/Login" };

            foreach (var page in authPages)
            {
                if (path.EndsWith(page, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
            }

            return returnUrl;
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            var destination = SafeReturnUrl(returnUrl);
            if (destination != null)
            {
                return LocalRedirect(destination);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
