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

    private async Task SendLink()
    {
        IsBusy = true;
        Message = null;
        LoginLink = null;

        try
        {
            // Magic-Link senden
            var result = await MagicLinkAuthService.SendLoginLinkAsync(Email);

            if (!result.Success)
            {
                Message = "Diese E-Mail-Adresse ist nicht autorisiert.";

                return;
            }

            if (result.LoginLink != null)
            {
                LoginLink = result.LoginLink;
                Message = "Der Anmeldelink wurde lokal erstellt.";
            }
            else
            {
                Message = "Der Anmeldelink wurde gesendet. " +
                          "Bitte überprüfen Sie Ihr E-Mail-Postfach.";
            }

        }
        finally
        {
            IsBusy = false;
        }
    }
}