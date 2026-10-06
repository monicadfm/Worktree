namespace Worktree.Helpers
{
    public interface IMailHelper
    {
        Response SendEmail(string to, string subject, string body);
    }
}
