using PresenceFlow.Auth;
using PresenceFlow.DataAccessLayer;

namespace PresenceFlow.Services
{
    public class MagicLinkAuthService : IMagicLinkAuthService
    {
        private readonly IPresenceRepository _repository;
        private readonly IServiceProvider _serviceProvider;
        private readonly LoginTokenStore _tokenStore;
        private readonly string _baseUrl;
        private readonly string _magicLinkProvider;

        // Konstruktor: Abhängigkeiten und BaseUrl laden
        public MagicLinkAuthService(IPresenceRepository repository, IServiceProvider serviceProvider, LoginTokenStore tokenStore, IConfiguration config)
        {
            _repository = repository;
            _serviceProvider = serviceProvider;
            _tokenStore = tokenStore;
            _baseUrl = config["App:BaseUrl"]
                ?? throw new InvalidOperationException("App:BaseUrl fehlt.");

            _magicLinkProvider = config["MagicLink:Provider"]
                ?? throw new InvalidOperationException("MagicLink:Provider fehlt.");
        }

        // Sendet einen einmaligen Login-Link an die angegebene E-Mail
        public async Task<MagicLinkSendResult> SendLoginLinkAsync(string email)
        {
            // Prüfen, ob die Person existiert
            var person = await _repository.GetPersonAsync(email);

            if (person == null)
            {
                return new MagicLinkSendResult
                {
                    Success = false
                };
            }

            // Token erzeugen (GUID ohne Bindestriche) -> besser für URLs
            var token = Guid.NewGuid().ToString("N");

            var lifetime = TimeSpan.FromMinutes(10);
            var lifetimeMinutes = (int)lifetime.TotalMinutes;

            // Token im Speicher ablegen
            _tokenStore.Add(token, email, lifetime);

            // Login-Link zusammenstellen
            var relativeLink = $"/login/magic?token={token}";

            if (_magicLinkProvider.Equals("UI", StringComparison.OrdinalIgnoreCase))
            {
                return new MagicLinkSendResult
                {
                    Success = true,
                    LoginLink = relativeLink
                };
            }

            if (_magicLinkProvider.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                var absoluteLink = $"{_baseUrl.TrimEnd('/')}{relativeLink}";

                var textBody = CreateTextBody(
                    absoluteLink,
                    lifetimeMinutes);

                var htmlBody = CreateHtmlBody(
                    person.FirstName,
                    absoluteLink,
                    lifetimeMinutes);

                // E-Mail-Service nur bei Bedarf aus dem DI-Container laden
                var emailService = _serviceProvider.GetRequiredService<IEmailService>();

                await emailService.SendAsync(
                    email,
                    "Dein Login-Link",
                    textBody,
                    htmlBody
                );

                return new MagicLinkSendResult
                {
                    Success = true
                };
            }

            throw new InvalidOperationException(
                    $"Unbekannter Magic-Link-Provider: " +
                    $"'{_magicLinkProvider}'. " +
                    "Erlaubte Werte sind 'UI' " +
                    "und 'Email'.");
        }

        // Prüft und konsumiert (entfernt) einen Token, gibt die zugehörige E-Mail zurück
        public bool ConsumeToken(string token, out string email)
            => _tokenStore.TryConsume(token, out email);

        // Textversion der E-Mail
        private static string CreateTextBody(
        string link,
        int lifetimeMinutes)
        {
            return $"""
            Anmeldung

            Öffnen Sie den folgenden Link, um sich anzumelden:

            {link}

            Der Link ist {lifetimeMinutes} Minuten gültig.

            Falls Sie diese E-Mail nicht angefordert haben,
            ignorieren Sie sie bitte.
            """;
        }

        // HTML-Version der E-Mail
        private static string CreateHtmlBody(
        string firstName,
        string link,
        int lifetimeMinutes)
        {
            return $"""
            <!DOCTYPE html>
            <html>
            <body style="
                margin:0;
                padding:0;
                background-color:#f4f6f9;
                font-family:Segoe UI, Tahoma, Arial, sans-serif;">

            <table width="100%"
                   cellpadding="0"
                   cellspacing="0">
                <tr>
                    <td align="center"
                        style="padding:24px;">

                        <table width="100%"
                               cellpadding="0"
                               cellspacing="0"
                               style="
                                   max-width:420px;
                                   background:#ffffff;
                                   border-radius:16px;
                                   padding:24px;
                                   box-shadow:
                                       0 10px 30px
                                       rgba(0,0,0,0.08);">

                            <tr>
                                <td style="text-align:center;">

                                    <h2 style="
                                        margin-top:0;
                                        color:#212529;">
                                        Anmeldung
                                    </h2>

                                    <p style="
                                        font-size:16px;
                                        color:#495057;">
                                        Hallo {firstName},
                                        klicken Sie auf den Button,
                                        um sich anzumelden.
                                    </p>

                                    <a href="{link}"
                                       style="
                                           display:inline-block;
                                           margin:24px 0;
                                           padding:16px 24px;
                                           background-color:#007bff;
                                           color:#ffffff;
                                           text-decoration:none;
                                           font-size:18px;
                                           font-weight:600;
                                           border-radius:12px;">
                                        Jetzt anmelden
                                    </a>

                                    <p style="
                                        font-size:14px;
                                        color:#6c757d;">
                                        Dieser Link ist
                                        <strong>
                                            {lifetimeMinutes} Minuten
                                        </strong>
                                        gültig.
                                    </p>

                                    <hr style="
                                        border:none;
                                        border-top:
                                            1px solid #e9ecef;
                                        margin:24px 0;">

                                    <p style="
                                        font-size:13px;
                                        color:#6c757d;">
                                        Falls der Button nicht
                                        funktioniert, kopieren Sie
                                        diesen Link in Ihren Browser:
                                    </p>

                                    <p style="
                                        font-size:13px;
                                        word-break:break-all;">
                                        <a href="{link}"
                                           style="color:#007bff;">
                                            {link}
                                        </a>
                                    </p>

                                </td>
                            </tr>
                        </table>

                        <p style="
                            font-size:12px;
                            color:#adb5bd;
                            margin-top:16px;">
                            Diese E-Mail wurde automatisch
                            erstellt. Bitte antworten Sie
                            nicht darauf.
                        </p>

                    </td>
                </tr>
            </table>

            </body>
            </html>
            """;
        }
    }
}