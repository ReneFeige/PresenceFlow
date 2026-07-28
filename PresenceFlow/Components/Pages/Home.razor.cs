using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using PresenceFlow.Models;
using PresenceFlow.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace PresenceFlow.Components.Pages;

public partial class Home : IAsyncDisposable
{
    [Inject]
    private IPresenceService PresenceService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    // SignalR-Verbindung
    private HubConnection? _hubConnection;

    private int PresentCount { get; set; }
    private Person? Person { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsStorageAvailable { get; set; } = true;
    private bool Success { get; set; } = true;
    private ClaimsPrincipal CurrentUser { get; set; } = new(new ClaimsIdentity());

    private string ButtonCssClass =>
        Person?.Status == PresenceStatus.Absent
            ? "btn-success"
            : "btn-danger";

    private string ButtonText =>
        Person?.Status == PresenceStatus.Present
            ? "Abmelden"
            : "Anmelden";

    protected override async Task OnInitializedAsync()
    {
        await CheckStorageAvailabilityAsync();

        if (!IsStorageAvailable)
        {
            IsLoading = false;
            return;
        }

        // Authentifizierungscookie prüfen
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        CurrentUser = authenticationState.User;

        var email = CurrentUser.FindFirstValue(ClaimTypes.Email);

        if (!string.IsNullOrWhiteSpace(email))
        {
            Person = await PresenceService.GetPersonAsync(email);
        }

        PresentCount = await PresenceService.GetPresentCountAsync();

        await InitializeHubConnectionAsync();

        IsLoading = false;
    }

    // SignalR-Verbindung aufbauen
    private async Task InitializeHubConnectionAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/presenceHub"))
            .WithAutomaticReconnect()
            .Build();

        // Listener für Updates von anderen Clients registrieren
        _hubConnection.On<PresenceUpdateDto>("ReceiveUpdate", async update =>
            {
                PresentCount = update.PresentCount;

                if (Person != null)
                {
                    Person = await PresenceService.GetPersonAsync(Person.Email);
                }

                // UI aktualisieren
                await InvokeAsync(StateHasChanged);
            });

        // SignalR-Verbindung starten
        await _hubConnection.StartAsync();
    }

    private async Task LoginLogout()
    {
        if (Person == null || CurrentUser.Identity?.IsAuthenticated != true)
        {
            Success = false;
            return;
        }

        var newStatus =
            Person.Status == PresenceStatus.Absent
                ? PresenceStatus.Present
                : PresenceStatus.Absent;

        Success = await PresenceService.SetPresenceForCurrentUserAsync(CurrentUser, newStatus);

        if (!Success)
        {
            return;
        }

        // E-Mail des aktuellen Benutzers aus den Claims auslesen
        var email = CurrentUser.FindFirstValue(ClaimTypes.Email);

        if (!string.IsNullOrWhiteSpace(email))
        {
            // Status erneut vom Service laden, damit die UI sofort den aktuellen Status anzeigt
            Person = await PresenceService.GetPersonAsync(email);
        }
    }

    private async Task LogoutFromApp()
    {
        var email = CurrentUser.FindFirstValue(ClaimTypes.Email);

        if (!string.IsNullOrWhiteSpace(email))
        {
            var currentPerson = await PresenceService.GetPersonAsync(email);

            if (currentPerson != null)
            {
                // authVersion erhöhen -> alle bestehenden Cookies werden ungültig
                await PresenceService.UpdatePersonAuthVersionAsync(email, currentPerson.AuthVersion + 1);
            }
        }

        await DisposeHubConnectionAsync();

        // Vollständiger Reload -> Cookie wird neu geprüft
        NavigationManager.NavigateTo("/auth/logout", forceLoad: true);
    }

    private async Task CheckStorageAvailabilityAsync()
    {
        try
        {
            await PresenceService.GetPeopleAsync();

            IsStorageAvailable = true;
        }
        catch
        {
            IsStorageAvailable = false;
        }
    }

    private async Task DisposeHubConnectionAsync()
    {
        if (_hubConnection == null)
        {
            return;
        }

        // SignalR-Verbindung schließen
        await _hubConnection.DisposeAsync();

        _hubConnection = null;
    }

    // Dispose-Methode für asynchrone Bereinigung
    public async ValueTask DisposeAsync()
    {
        await DisposeHubConnectionAsync();
    }
}