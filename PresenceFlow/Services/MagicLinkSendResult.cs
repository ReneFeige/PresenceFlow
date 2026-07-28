namespace PresenceFlow.Services
{
    public class MagicLinkSendResult
    {
        public bool Success { get; init; }
        public bool IsUiDemo { get; init; }
        public string? LoginLink { get; init; }
    }
}
