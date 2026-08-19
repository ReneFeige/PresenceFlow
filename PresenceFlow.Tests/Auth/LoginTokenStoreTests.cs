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

        [Fact]
        public void TryConsume_ValidToken_CanOnlyBeConsumedOnce()
        {
            var store = new LoginTokenStore();
            store.Add(
                "test-token",
                "user@example.com",
                TimeSpan.FromMinutes(10)
            );

            var firstAttempt = store.TryConsume(
                "test-token",
                out var firstEmail
            );

            var secondAttempt = store.TryConsume(
                "test-token",
                out var secondEmail
            );

            Assert.True(firstAttempt);
            Assert.Equal("user@example.com", firstEmail);

            Assert.False(secondAttempt);
            Assert.Equal(string.Empty, secondEmail);
        }

        [Fact]
        public void TryConsume_UnknownToken_ReturnsFalse()
        {
            var store = new LoginTokenStore();

            var success = store.TryConsume(
                "unknown-token",
                out var email
            );

            Assert.False(success);
            Assert.Equal(string.Empty, email);
        }

        [Fact]
        public void Add_NewTokenForSameEmail_InvalidatesPreviousToken()
        {
            var store = new LoginTokenStore();
            store.Add(
                "first-token",
                "user@example.com",
                TimeSpan.FromMinutes(10)
            );

            store.Add(
                "second-token",
                "user@example.com",
                TimeSpan.FromMinutes(10)
            );

            var oldTokenSuccess = store.TryConsume(
                "first-token",
                out _
            );

            var newTokenSuccess = store.TryConsume(
                "second-token",
                out var secondEmail
            );

            Assert.False(oldTokenSuccess);
            Assert.True(newTokenSuccess);
            Assert.Equal("user@example.com", secondEmail);
        }

        [Fact]
        public void Remove_ExistingToken_InvalidatesToken()
        {
            var store = new LoginTokenStore();

            store.Add(
                "test-token",
                "user@example.com",
                TimeSpan.FromMinutes(10)
            );

            var removed = store.Remove("test-token");

            var success = store.TryConsume(
                "test-token",
                out var email
            );

            Assert.True(removed);
            Assert.False(success);
            Assert.Equal(string.Empty, email);
        }

        [Fact]
        public void Remove_UnknownToken_ReturnsFalse()
        {
            var store = new LoginTokenStore();

            var removed = store.Remove("unknown-token");

            Assert.False(removed);
        }

        [Fact]
        public void TryConsume_ExpiredToken_ReturnsFalse()
        {
            var store = new LoginTokenStore();
            store.Add(
                "test-token",
                "user@example.com",
                TimeSpan.FromMinutes(-1) // Token ist bereits beim Speichern abgelaufen
            );

            var success = store.TryConsume(
                "test-token",
                out var email
            );

            Assert.False(success);
            Assert.Equal(string.Empty, email);
        }

        [Fact]
        public void TryConsume_ExpiredToken_RemovesToken()
        {
            var store = new LoginTokenStore();

            store.Add(
                "expired-token",
                "user@example.com",
                TimeSpan.FromMinutes(-1) // Token ist bereits beim Speichern abgelaufen
            );

            var firstAttempt = store.TryConsume(
                "expired-token",
                out _
            );

            // Token sollte nach dem ersten Versuch entfernt worden sein
            var removedAfterAttempt = store.Remove("expired-token");

            Assert.False(firstAttempt);
            Assert.False(removedAfterAttempt);
        }
    }
}