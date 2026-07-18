using PresenceFlow.Auth;

namespace PresenceFlow.Services
{
    public interface IAuthCookieService
    {
        Task SignInAsync(string email);
        Task<AuthCookie?> GetAuthCookieAsync();
        Task SignOutAsync();
        Task RefreshAsync();
    }
}
