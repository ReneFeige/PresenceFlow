using PresenceFlow.Auth;

namespace PresenceFlow.Services
{
    public interface IAuthCookieService
    {
        Task<bool> SignInAsync(string email);
        Task<AuthCookie?> GetAuthCookieAsync();
        Task SignOutAsync();
    }
}
