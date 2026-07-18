namespace PresenceFlow.Auth
{
    public class AuthCookie
    {
        public string Email { get; set; } = default!;
        public int AuthVersion { get; set; }
    }
}
