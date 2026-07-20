namespace PresenceFlow.Services
{
    public interface IMagicLinkAuthService
    {
        Task<bool> SendLoginLinkAsync(string email);
        bool ConsumeToken(string token, out string email);
    }
}
