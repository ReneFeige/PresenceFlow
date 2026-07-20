using PresenceFlow.Auth;
using PresenceFlow.DataAccessLayer;

namespace PresenceFlow.Services
{
    public class MagicLinkAuthService : IMagicLinkAuthService
    {
        private readonly IPresenceRepository _repository;
        private readonly IEmailService _emailService;
        private readonly LoginTokenStore _tokenStore;
        private readonly string _baseUrl;

        // Konstruktor: Abhängigkeiten und BaseUrl laden
        public MagicLinkAuthService(IPresenceRepository repository, IEmailService emailService, LoginTokenStore tokenStore, IConfiguration config)
        {
            _repository = repository;
            _emailService = emailService;
            _tokenStore = tokenStore;
            _baseUrl = config["App:BaseUrl"]
                ?? throw new InvalidOperationException("App:BaseUrl missing");
        }

        // Sendet einen einmaligen Login-Link an die angegebene E-Mail
        public async Task<bool> SendLoginLinkAsync(string email)
        {
            // Prüfen, ob die Person existiert
            var person = await _repository.GetPersonAsync(email);

            if (person == null)
            { 
                return false;
            }

            // Token erzeugen (GUID ohne Bindestriche) -> besser für URLs
            var token = Guid.NewGuid().ToString("N");

            var lifetime = TimeSpan.FromMinutes(10);
            var lifetimeMinutes = (int)lifetime.TotalMinutes;

            // Token im Speicher ablegen
            _tokenStore.Add(token, email, lifetime);

            // Login-Link zusammenstellen
            var link = $"{_baseUrl}/login/magic?token={token}";

            // Textversion der E-Mail
            var textBody = $@"Anmeldung

Öffnen Sie den folgenden Link, um sich anzumelden:

{link}

Der Link ist {lifetimeMinutes} Minuten gültig.

Falls Sie diese E-Mail nicht angefordert haben, ignorieren Sie sie bitte.";

            // HTML-Version der E-Mail
            var htmlBody = $@"<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background-color:#f4f6f9;font-family:Segoe UI, Tahoma, Arial,sans-serif;'>

<table width='100%' cellpadding='0' cellspacing='0'>
<tr>
<td align='center' style='padding:24px;'>

<table width='100%' max-width='420' cellpadding='0' cellspacing='0'
       style='background:#ffffff;border-radius:16px;padding:24px;box-shadow:0 10px 30px rgba(0,0,0,0.08);'>

<tr>
<td style='text-align:center;'>

<h2 style='margin-top:0;color:#212529;'>Anmeldung</h2>

<p style='font-size:16px;color:#495057;'>
Hallo {person.FirstName}, klicken Sie auf den Button, um sich anzumelden.
</p>

<a href='{link}'
   style='display:inline-block;margin:24px 0;padding:16px 24px;background-color:#007bff;color:#ffffff;text-decoration:none;font-size:18px;font-weight:600;border-radius:12px;'>
Jetzt anmelden
</a>

<p style='font-size:14px;color:#6c757d;'>Dieser Link ist <strong>{lifetimeMinutes} Minuten</strong> gültig.</p>

<hr style='border:none;border-top:1px solid #e9ecef;margin:24px 0;'>

<p style='font-size:13px;color:#6c757d;'>
Falls der Button nicht funktioniert, kopieren Sie diesen Link in Ihren Browser:
</p>

<p style='font-size:13px;word-break:break-all;'>
<a href='{link}' style='color:#007bff;'>{link}</a>
</p>

</td>
</tr>
</table>

<p style='font-size:12px;color:#adb5bd;margin-top:16px;'>
Diese E-Mail wurde automatisch erstellt. Bitte antworten Sie nicht darauf.
</p>

</td>
</tr>
</table>

</body>
</html>";

            await _emailService.SendAsync(
                email,
                "Dein Login-Link",
                textBody,
                htmlBody
            );

            return true;
        }

        // Prüft und konsumiert (entfernt) einen Token, gibt die zugehörige E-Mail zurück
        public bool ConsumeToken(string token, out string email)
            => _tokenStore.TryConsume(token, out email);
    }
}
