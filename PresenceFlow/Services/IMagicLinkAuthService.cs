namespace PresenceFlow.Services
{
    public interface IMagicLinkAuthService
    {
        Task<bool> SendLoginLinkAsync(string email);
        bool ConsumeTokenAsync(string token, out string email);
    }
}
