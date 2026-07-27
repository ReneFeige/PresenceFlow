using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PresenceFlow.Auth;
using PresenceFlow.DataAccessLayer;
using System.Security.Claims;

namespace PresenceFlow.Services
{
    public class AuthCookieService : IAuthCookieService
    {
        // IHttpContextAccessor ermöglicht den Zugriff auf den aktuellen HttpContext außerhalb von Controllern oder Middleware.
        // Wird hier benötigt, um Cookies zu setzen, zu lesen oder zu löschen (für Auth-Cookies).
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPresenceRepository _repository;

        private const string AuthVersionClaimType = "auth_version";

        public AuthCookieService(IHttpContextAccessor httpContextAccessor, IPresenceRepository repository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = repository;
        }

        public async Task<bool> SignInAsync(string email)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                return false;
            }

            // Person laden, um die aktuelle AuthVersion zu bekommen
            var person = await _repository.GetPersonAsync(email);

            if (person == null)
            {
                return false;
            }

            // Claims für die Authentifizierung erstellen.
            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, person.Id.ToString()),
            new(ClaimTypes.Name, $"{person.FirstName} {person.LastName}"),
            new(ClaimTypes.Email, person.Email),
            new(AuthVersionClaimType, person.AuthVersion.ToString())
        };

            // Administratorrolle hinzufügen.
            if (person.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            // Claims in einer Identität zusammenfassen.
            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties);

            return true;
        }

        public async Task<AuthCookie?> GetAuthCookieAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var principal = httpContext?.User;

            // Nur authentifizierte Benutzer zulassen.
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var email = principal.FindFirstValue(ClaimTypes.Email);
            var authVersionValue = principal.FindFirstValue(AuthVersionClaimType);

            if (string.IsNullOrWhiteSpace(email) || !int.TryParse(authVersionValue, out var authVersion))
            {
                return null;
            }

            var person = await _repository.GetPersonAsync(email);

            if (person == null || person.AuthVersion != authVersion)
            {
                return null;
            }

            // Gültige Authentifizierungsdaten zurückgeben.
            return new AuthCookie
            {
                Email = person.Email,
                AuthVersion = person.AuthVersion
            };
        }

        public async Task SignOutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                return;
            }

            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
