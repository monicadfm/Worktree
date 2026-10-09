using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Worktree.Data.Entities;
using Worktree.Models;

namespace Worktree.Helpers
{
    public interface IUserHelper
    {
        Task<User?> GetUserByEmailAsync(string email);

        Task<IdentityResult> AddUserAsync(User user, string password);

        Task<SignInResult> LoginAsync(LoginViewModel model);

        Task LogoutAsync();

        Task<IdentityResult> UpdateUserAsync(User user);

        Task<IdentityResult> ChangePasswordAsync(User user, string oldPassword, string newPassword);

        Task CheckRoleAsync(string roleName);

        Task AddUserToRoleAsync(User user, string roleName);

        Task<bool> IsUserInRoleAsync(User user, string roleName);

        Task<User?> GetUserWithProfileAsync(string email);

        Task<string> GeneratePasswordResetTokenAsync(User user);

        Task<IdentityResult> ResetPasswordAsync(User user, string token, string password);

        Task<IdentityResult> AddUserAsync(User user);

        Task<IEnumerable<AuthenticationScheme>> GetExternalLoginsAsync();

        AuthenticationProperties ConfigureExternalLogin(string provider, string redirectUrl);

        Task<ExternalLoginInfo?> GetExternalLoginInfoAsync();

        Task<SignInResult> ExternalLoginSignInAsync(ExternalLoginInfo info);

        Task<IdentityResult> AddLoginAsync(User user, ExternalLoginInfo info);

        Task SignInAsync(User user);
    }
}