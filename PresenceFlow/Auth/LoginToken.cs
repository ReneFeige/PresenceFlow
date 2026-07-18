namespace PresenceFlow.Auth
{
    public class LoginToken
    {
        public string Email { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }
}
