namespace PresenceFlow.Services
{
    public interface IMagicLinkAuthService
    {
        Task<MagicLinkSendResult> SendLoginLinkAsync(string email);
        bool ConsumeToken(string token, out string email);
    }
}
