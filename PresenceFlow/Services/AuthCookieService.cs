using System.Text.Json;
using PresenceFlow.Auth;
using PresenceFlow.DataAccessLayer;

namespace PresenceFlow.Services
{
    public class AuthCookieService : IAuthCookieService
    {
        // IHttpContextAccessor ermöglicht den Zugriff auf den aktuellen HttpContext außerhalb von Controllern oder Middleware.
        // Wird hier benötigt, um Cookies zu setzen, zu lesen oder zu löschen (für Auth-Cookies).
        private readonly IHttpContextAccessor _httpContext;
        private readonly IPresenceRepository _repository;
        private const string CookieName = "PresenceFlowAuth";

        public AuthCookieService(IHttpContextAccessor httpContext, IPresenceRepository repository)
        {
            _httpContext = httpContext;
            _repository = repository;
        }

        public async Task<bool> SignInAsync(string email)
        {
            // Person laden, um die aktuelle AuthVersion zu bekommen
            var person = await _repository.GetPersonAsync(email);
            var httpContext = _httpContext.HttpContext;

            if (person == null || httpContext == null)
            {
                return false;
            }

            var cookie = new AuthCookie
            {
                Email = person.Email,
                AuthVersion = person.AuthVersion
            };

            var cookieValue = JsonSerializer.Serialize(cookie);

            httpContext.Response.Cookies.Append(
                CookieName,
                cookieValue,
                CreateCookieOptions(
                    SameSiteMode.Lax));

            return true;
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
                var cookie = JsonSerializer.Deserialize<AuthCookie>(cookieValue);

                if (cookie == null || string.IsNullOrWhiteSpace(cookie.Email))
                {
                    return null;
                }

                // Aktuelle Person aus Datenbank holen
                var person = await _repository.GetPersonAsync(cookie.Email);

                if (person == null)
                {
                    return null;
                }

                // AuthVersion prüfen
                if (cookie.AuthVersion != person.AuthVersion)
                {
                    return null;
                }

                // Alles gut -> Cookie zurückgeben
                return cookie;
            }
            catch (JsonException)
            {
                // Ungültiges Cookie-Format -> ignorieren
                return null;
            }
        }

        public Task SignOutAsync()
        {
            var httpContext = _httpContext.HttpContext;

            if (httpContext != null)
            {
                // Auth-Cookie löschen -> Benutzer ist abgemeldet
                httpContext.Response.Cookies.Delete(
                    CookieName,
                    CreateCookieOptions(
                        SameSiteMode.Lax));
            }
            // Aktuell keine asynchrone Methode
            // Durch Task.CompletedTask ist die Methode awaitable
            // Abgeschlossener Task wird zurückgegeben
            return Task.CompletedTask;
        }

        public async Task RefreshAsync()
        {
            var httpContext = _httpContext.HttpContext;

            if (httpContext == null)
            {
                // Kein aktiver HTTP-Kontext (z.B. Background-Thread)
                return;
            }

            var cookieExists = httpContext.Request.Cookies.ContainsKey(CookieName);

            if (!cookieExists)
            {
                return;
            }

            // Bestehendes Auth-Cookie lesen und vollständig validieren
            // (Existenz, Person vorhanden, AuthVersion aktuell)
            var auth = await GetAuthCookieAsync();

            // Kein gültiges Cookie vorhanden -> nichts verlängern
            if (auth == null)
            {
                await SignOutAsync();
                return;
            }

            // Cookie neu setzen (gleiches Payload, neues Ablaufdatum)
            var cookieValue = JsonSerializer.Serialize(auth);

            httpContext.Response.Cookies.Append(
                CookieName,
                cookieValue,
                CreateCookieOptions(
                    SameSiteMode.Lax));
        }

        private static CookieOptions CreateCookieOptions(
            SameSiteMode sameSite)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = sameSite,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            };
        }
    }
}
