using PresenceFlow.Auth;

namespace PresenceFlow.Services
{
    public class AuthCookieService : IAuthCookieService
    {
        // IHttpContextAccessor ermöglicht den Zugriff auf den aktuellen HttpContext außerhalb von Controllern oder Middleware.
        // Wird hier benötigt, um Cookies zu setzen, zu lesen oder zu löschen (für Auth-Cookies).
        private readonly IHttpContextAccessor _httpContext;
        private readonly IPresenceService _repository;
        private const string CookieName = "IoBrokerWebAppAuth";

        public AuthCookieService(IHttpContextAccessor httpContext, IPresenceService repository)
        {
            _httpContext = httpContext;
            _repository = repository;
        }

        public async Task SignInAsync(string email)
        {
            // Person aus ioBroker laden, um die aktuelle AuthVersion zu bekommen
            var people = await _repository.GetPeopleAsync();
            var person = people.SingleOrDefault(p => p.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (person == null || _httpContext.HttpContext == null)
            {
                return;
            }

            var cookieObj = new AuthCookie
            {
                Email = person.Email,
                AuthVersion = person.AuthVersion
            };

            var cookieValue = System.Text.Json.JsonSerializer.Serialize(cookieObj);

            _httpContext.HttpContext.Response.Cookies.Append(
                CookieName,
                cookieValue,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.Now.AddDays(30)
                }
            );
        }

        public async Task<AuthCookie?> GetAuthCookieAsync()
        {
            var cookieValue = _httpContext.HttpContext?.Request.Cookies[CookieName];
            if (string.IsNullOrWhiteSpace(cookieValue))
            {
                return null;
            }

            try
            {
                var cookie = System.Text.Json.JsonSerializer.Deserialize<AuthCookie>(cookieValue);
                if (cookie == null)
                {
                    return null;
                }

                // Aktuelle Person aus ioBroker holen
                var people = await _repository.GetPeopleAsync();
                var person = people.SingleOrDefault(p => p.Email.Equals(cookie.Email, StringComparison.OrdinalIgnoreCase));

                if (person == null)
                {
                    // Person existiert nicht mehr -> Cookie löschen / abmelden
                    await SignOutAsync();
                    return null;
                }

                // AuthVersion prüfen
                if (cookie.AuthVersion != person.AuthVersion)
                {
                    // AuthVersion nicht mehr gültig -> Cookie löschen / abmelden
                    await SignOutAsync();
                    return null;
                }

                // Alles gut -> Cookie zurückgeben
                return cookie;
            }
            catch
            {
                // Ungültiges Cookie-Format -> ignorieren
                return null;
            }
        }

        public Task SignOutAsync()
        {
            if (_httpContext.HttpContext != null)
            {
                // Auth-Cookie löschen -> Benutzer ist abgemeldet
                _httpContext.HttpContext.Response.Cookies.Delete(CookieName);
            }
            // Aktuell keine asynchrone Methode
            // Durch Task.CompletedTask ist die Methode awaitable
            // Abgeschlossener Task wird zurückgegeben
            return Task.CompletedTask;
        }

        public async Task RefreshAsync()
        {
            if (_httpContext.HttpContext == null)
            {
                // Kein aktiver HTTP-Kontext (z.B. Background-Thread)
                return;
            }

            // Bestehendes Auth-Cookie lesen und vollständig validieren
            // (Existenz, Person vorhanden, AuthVersion aktuell)
            var auth = await GetAuthCookieAsync();

            // Kein gültiges Cookie vorhanden -> nichts verlängern
            if (auth == null)
            {
                return;
            }

            // Cookie neu setzen (gleiches Payload, neues Ablaufdatum)
            var cookieValue = System.Text.Json.JsonSerializer.Serialize(auth);

            _httpContext.HttpContext.Response.Cookies.Append(
                CookieName,
                cookieValue,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.Now.AddDays(30)
                }
            );
        }
    }
}
