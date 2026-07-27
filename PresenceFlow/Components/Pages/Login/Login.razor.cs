using Microsoft.AspNetCore.Components;
using PresenceFlow.Services;

namespace PresenceFlow.Components.Pages.Login;

public partial class Login
{
    [Inject]
    private IMagicLinkAuthService MagicLinkAuthService { get; set; } = default!;

    private string Email { get; set; } = string.Empty;
    private string? Message { get; set; }
    private string? LoginLink { get; set; }
    private bool IsBusy { get; set; }
    private LoginMessageType MessageType { get; set; }

    private string MessageCssClass =>
        MessageType switch
        {
            LoginMessageType.Error => "alert alert-danger",
            LoginMessageType.Success => "alert alert-success",
            LoginMessageType.Warning => "alert alert-warning",
            _ => "alert alert-secondary"
        };

    private async Task SendLink()
    {
        Message = null;
        LoginLink = null;
        MessageType = LoginMessageType.None;

        if (string.IsNullOrWhiteSpace(Email))
        {
            Message = "Bitte geben Sie eine E-Mail-Adresse ein.";

            MessageType = LoginMessageType.Error;

            return;
        }

        IsBusy = true;

        try
        {
            // Magic-Link senden
            var result = await MagicLinkAuthService.SendLoginLinkAsync(Email);

            if (!result.Success)
            {
                Message = "Mit dieser E-Mail-Adresse ist keine Anmeldung möglich. Bitte prüfen Sie Ihre Eingabe.";

                MessageType = LoginMessageType.Error;

                return;
            }

            if (result.LoginLink != null)
            {
                LoginLink = result.LoginLink;
                Message = "Der Anmeldelink wurde lokal erstellt.";

                MessageType = LoginMessageType.Warning;
            }
            else
            {
                Message = "Der Anmeldelink wurde per E-Mail versendet. " +
                          "Bitte prüfen Sie Ihr Postfach.";

                MessageType = LoginMessageType.Success;
            }

        }
        finally
        {
            IsBusy = false;
        }
    }

    private enum LoginMessageType
    {
        None,
        Success,
        Warning,
        Error
    }
}