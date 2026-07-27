using Azure;
using Azure.Communication.Email;

namespace PresenceFlow.Services
{
    public class AzureEmailService : IEmailService
    {
        // Azure EmailClient für den Versand von E-Mails
        private readonly EmailClient _emailClient;
        private readonly string _sender;

        // Konstruktor: ConnectionString und Absender der Konfiguration laden
        public AzureEmailService(IConfiguration config)
        {
            var connectionString = config["AzureEmail:ConnectionString"]
                ?? throw new InvalidOperationException("Email ConnectionString missing");

            _sender = config["AzureEmail:SenderAddress"]
                ?? throw new InvalidOperationException("Email SenderAddress missing");

            _emailClient = new EmailClient(connectionString);
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string plainTextBody,
            string htmlBody)
        {
            // Nachricht erstellen
            var message = new EmailMessage(senderAddress: _sender,
                content: new EmailContent(subject)
                {
                    PlainText = plainTextBody,
                    Html = htmlBody
                },
                recipients: new EmailRecipients(new List<EmailAddress>
                    {
                    new EmailAddress(toEmail)
                    }));

            // Nachricht versenden
            await _emailClient.SendAsync(WaitUntil.Completed, message);
        }
    }
}
