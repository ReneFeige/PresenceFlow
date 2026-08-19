using PresenceFlow.Auth;

namespace PresenceFlow.Tests.Auth
{
    public class LoginTokenStoreTests
    {
        [Fact]
        public void TryConsume_ValidToken_ReturnsEmail()
        {
            var store = new LoginTokenStore();

            store.Add(
                "test-token",
                "user@example.com",
                TimeSpan.FromMinutes(10)
            );

            var success = store.TryConsume(
                "test-token",
                out var email);

            Assert.True(success);
            Assert.Equal("user@example.com", email);
        }
    }
}