using System.Collections.Concurrent;

namespace PresenceFlow.Auth
{
    public class LoginTokenStore
    {
        // Dictionary Schreiben und Lesen, von mehreren Instanzen gleichzeitig, ohne Fehler, ermöglicht
        private readonly ConcurrentDictionary<string, LoginToken> _tokens = new();

        public void Add(string token, string email, TimeSpan lifetime)
        {
            // alte Tokens für die gleiche Email entfernen
            foreach (var kvp in _tokens.Where(k => k.Value.Email == email))
            { 
                _tokens.TryRemove(kvp.Key, out _);
            }

            _tokens[token] = new LoginToken
            {
                Email = email,
                ExpiresAt = DateTime.Now.Add(lifetime)
            };
        }

        public bool TryConsume(string token, out string email)
        {
            email = string.Empty;

            if (_tokens.TryGetValue(token, out var entry))
            {
                if (entry.ExpiresAt > DateTime.Now)
                {
                    // Token gültig -> einmalig entfernen
                    _tokens.TryRemove(token, out _);
                    email = entry.Email;
                    return true;
                }

                // Token abgelaufen -> entfernen
                _tokens.TryRemove(token, out _);
            }

            return false;
        }
    }
}
