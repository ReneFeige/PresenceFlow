using System.Text.Encodings.Web;
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
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var isUiProvider = _magicLinkProvider.Equals("UI", StringComparison.OrdinalIgnoreCase);

            var isEmailProvider = _magicLinkProvider.Equals("Email", StringComparison.OrdinalIgnoreCase);

            if (!isUiProvider && !isEmailProvider)
            {
                throw new InvalidOperationException(
                    $"Unbekannter Magic-Link-Provider: '{_magicLinkProvider}'. " +
                    "Erlaubte Werte sind 'UI' und 'Email'.");
            }

            // Prüfen, ob die Person existiert
            var person = await _repository.GetPersonAsync(email);

            if (person == null)
            {
                // Der lokale UI-Demomodus darf mitteilen, dass kein Demo-Benutzer
                // für die eingegebene Adresse vorhanden ist.
                if (isUiProvider)
                {
                    return new MagicLinkSendResult
                    {
                        Success = false,
                        IsUiDemo = true
                    };
                }

                // Im normalen E-Mail-Modus immer ein neutrales Ergebnis liefern.
                // Dadurch ist von außen nicht erkennbar, ob die Adresse existiert.
                return new MagicLinkSendResult
                {
                    Success = true,
                    IsUiDemo = false
                };
            }

            // Token erzeugen (GUID ohne Bindestriche) -> besser für URLs
            var token = Guid.NewGuid().ToString("N");

            var lifetime = TimeSpan.FromMinutes(10);
            var lifetimeMinutes = (int)lifetime.TotalMinutes;

            // Token im Speicher ablegen
            _tokenStore.Add(token, person.Email, lifetime);

            // Login-Link zusammenstellen
            var relativeLink = $"/auth/magic?token={token}";

            if (isUiProvider)
            {
                return new MagicLinkSendResult
                {
                    Success = true,
                    IsUiDemo = true,
                    LoginLink = relativeLink
                };
            }

            var absoluteLink = $"{_baseUrl.TrimEnd('/')}{relativeLink}";

            var textBody = CreateTextBody(
                absoluteLink,
                lifetimeMinutes);

            var htmlBody = CreateHtmlBody(
                person.FirstName,
                absoluteLink,
                lifetimeMinutes);

            // E-Mail-Service nur bei Bedarf aus dem DI-Container laden
            using var scope = _serviceProvider.CreateScope();

            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            await emailService.SendAsync(
                person.Email,
                "Ihr Login-Link für PresenceFlow",
                textBody,
                htmlBody
            );

            return new MagicLinkSendResult
            {
                Success = true,
                IsUiDemo = false
            };
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
            Bei PresenceFlow anmelden

            Sie haben einen Login-Link für PresenceFlow angefordert.

            Öffnen Sie den folgenden Link, um sich anzumelden:

            {link}

            Der Link ist {lifetimeMinutes} Minuten gültig und kann nur einmal verwendet werden.

            Falls Sie diese Anmeldung nicht angefordert haben, können Sie diese E-Mail ignorieren.

            PresenceFlow
            Anwesenheit einfach erfassen
            """;
        }

        // HTML-Version der E-Mail
        private static string CreateHtmlBody(
            string firstName,
            string link,
            int lifetimeMinutes)
        {
            var encodedFirstName = HtmlEncoder.Default.Encode(firstName);
            var encodedLink = HtmlEncoder.Default.Encode(link);

            return $"""
            <!DOCTYPE html>
            <html lang="de">
            <head>
                <meta charset="utf-8">
                <meta name="viewport"
                      content="width=device-width, initial-scale=1">
                <meta name="color-scheme"
                      content="light">
                <meta name="supported-color-schemes"
                      content="light">

                <title>
                    Bei PresenceFlow anmelden
                </title>
            </head>

            <body style="
                margin:0;
                padding:0;
                background-color:#eef3f8;
                color:#172033;
                font-family:Segoe UI, Tahoma, Arial, sans-serif;">

                <div style="
                    display:none;
                    max-height:0;
                    overflow:hidden;
                    opacity:0;
                    color:transparent;">
                    Ihr persönlicher Login-Link für PresenceFlow ist
                    {lifetimeMinutes} Minuten gültig.
                </div>

                <table role="presentation"
                       width="100%"
                       cellpadding="0"
                       cellspacing="0"
                       border="0"
                       style="
                           width:100%;
                           background-color:#eef3f8;
                           border-collapse:collapse;">

                    <tr>
                        <td align="center"
                            style="padding:32px 16px;">

                            <table role="presentation"
                                   width="100%"
                                   cellpadding="0"
                                   cellspacing="0"
                                   border="0"
                                   style="
                                       width:100%;
                                       max-width:520px;
                                       overflow:hidden;
                                       background-color:#ffffff;
                                       border:1px solid #dce6f0;
                                       border-radius:20px;
                                       border-collapse:separate;
                                       box-shadow:0 18px 48px
                                           rgba(15, 23, 42, 0.10);">

                                <tr>
                                    <td style="
                                        height:6px;
                                        background-color:#007bff;
                                        background-image:
                                            linear-gradient(
                                                90deg,
                                                #007bff,
                                                #46a3ff
                                            );
                                        font-size:0;
                                        line-height:0;">
                                        &nbsp;
                                    </td>
                                </tr>

                                <tr>
                                    <td style="padding:34px 32px 28px;">

                                        <p style="
                                            margin:0 0 10px;
                                            color:#007bff;
                                            font-size:12px;
                                            font-weight:700;
                                            letter-spacing:1.2px;
                                            text-transform:uppercase;">
                                            Sicherer Zugang
                                        </p>

                                        <h1 style="
                                            margin:0 0 16px;
                                            color:#172033;
                                            font-size:28px;
                                            font-weight:700;
                                            line-height:1.25;">
                                            Bei PresenceFlow anmelden
                                        </h1>

                                        <p style="
                                            margin:0 0 12px;
                                            color:#475569;
                                            font-size:16px;
                                            line-height:1.65;">
                                            Hallo {encodedFirstName},
                                        </p>

                                        <p style="
                                            margin:0;
                                            color:#475569;
                                            font-size:16px;
                                            line-height:1.65;">
                                            klicken Sie auf den folgenden Button,
                                            um sich sicher bei PresenceFlow
                                            anzumelden.
                                        </p>

                                        <table role="presentation"
                                               width="100%"
                                               cellpadding="0"
                                               cellspacing="0"
                                               border="0"
                                               style="border-collapse:collapse;">

                                            <tr>
                                                <td align="center"
                                                    style="padding:28px 0;">

                                                    <a href="{encodedLink}"
                                                       style="
                                                           display:inline-block;
                                                           min-width:190px;
                                                           padding:16px 24px;
                                                           background-color:#007bff;
                                                           background-image:
                                                               linear-gradient(
                                                                   135deg,
                                                                   #007bff,
                                                                   #0569d8
                                                               );
                                                           border-radius:13px;
                                                           box-shadow:
                                                               0 10px 22px
                                                               rgba(
                                                                   0,
                                                                   123,
                                                                   255,
                                                                   0.22
                                                               );
                                                           color:#ffffff;
                                                           font-size:16px;
                                                           font-weight:700;
                                                           line-height:1.2;
                                                           text-align:center;
                                                           text-decoration:none;">
                                                        Jetzt anmelden
                                                    </a>

                                                </td>
                                            </tr>

                                        </table>

                                        <table role="presentation"
                                               width="100%"
                                               cellpadding="0"
                                               cellspacing="0"
                                               border="0"
                                               style="
                                                   width:100%;
                                                   background-color:#f5f9fd;
                                                   border:1px solid #d8e7f5;
                                                   border-radius:14px;
                                                   border-collapse:separate;">

                                            <tr>
                                                <td style="padding:16px 18px;">

                                                    <p style="
                                                        margin:0;
                                                        color:#36658f;
                                                        font-size:14px;
                                                        line-height:1.55;">
                                                        Dieser Link ist
                                                        <strong>
                                                            {lifetimeMinutes} Minuten
                                                        </strong>
                                                        gültig und kann nur einmal
                                                        verwendet werden.
                                                    </p>

                                                </td>
                                            </tr>

                                        </table>

                                        <div style="
                                            height:1px;
                                            margin:28px 0 22px;
                                            background-color:#e2e8f0;">
                                        </div>

                                        <p style="
                                            margin:0 0 10px;
                                            color:#64748b;
                                            font-size:13px;
                                            line-height:1.55;">
                                            Falls der Button nicht funktioniert,
                                            kopieren Sie den folgenden Link in
                                            Ihren Browser:
                                        </p>

                                        <p style="
                                            margin:0;
                                            overflow-wrap:anywhere;
                                            word-break:break-word;
                                            font-size:13px;
                                            line-height:1.55;">

                                            <a href="{encodedLink}"
                                               style="
                                                   color:#007bff;
                                                   text-decoration:underline;">
                                                {encodedLink}
                                            </a>

                                        </p>

                                    </td>
                                </tr>

                                <tr>
                                    <td style="
                                        padding:20px 32px;
                                        background-color:#f8fafc;
                                        border-top:1px solid #e2e8f0;">

                                        <p style="
                                            margin:0 0 6px;
                                            color:#334155;
                                            font-size:14px;
                                            font-weight:700;">
                                            PresenceFlow
                                        </p>

                                        <p style="
                                            margin:0;
                                            color:#64748b;
                                            font-size:12px;
                                            line-height:1.5;">
                                            Falls Sie diese Anmeldung nicht
                                            angefordert haben, können Sie diese
                                            E-Mail ignorieren.
                                        </p>

                                    </td>
                                </tr>

                            </table>

                            <p style="
                                max-width:520px;
                                margin:16px auto 0;
                                color:#94a3b8;
                                font-size:12px;
                                line-height:1.5;
                                text-align:center;">
                                Diese E-Mail wurde automatisch erstellt.
                                Bitte antworten Sie nicht darauf.
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