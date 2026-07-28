using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using PresenceFlow.Models;
using PresenceFlow.Services;

namespace PresenceFlow.Components.Pages;

public partial class ActualPresent : IAsyncDisposable
{
    [Inject]
    private IPresenceService PresenceService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    // SignalR-Verbindung
    private HubConnection? _hubConnection;

    private IReadOnlyList<Person> PresentPeople { get; set; } = Array.Empty<Person>();
    private int PresentCount => PresentPeople.Count;
    private bool IsLoading { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        // Initiale Daten vom Service laden (HTTP Request)
        PresentPeople = await PresenceService.GetPresentPeopleAsync();

        IsLoading = false;

        await InitializeHubConnectionAsync();
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
                PresentPeople = await PresenceService.GetPresentPeopleAsync();

                // UI aktualisieren
                await InvokeAsync(StateHasChanged);
            });

        // SignalR-Verbindung starten
        await _hubConnection.StartAsync();
    }

    private static string GetDisplayName(Person person)
    {
        var fullName = $"{person.FirstName} {person.LastName}".Trim();

        // Email-Adresse anzeigen, sollte der Name fehlen
        return string.IsNullOrWhiteSpace(fullName)
            ? "Unbekannte Person"
            : fullName;
    }

    private static string GetPresenceTimeText(Person person)
    {
        return person.Timestamp.HasValue
            ? $"Anwesend seit " +
              $"{person.Timestamp.Value:HH:mm} Uhr"
            : "Anwesenheitszeit nicht verfügbar";
    }

    // Dispose-Methode für asynchrone Bereinigung
    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            // SignalR-Verbindung schließen
            await _hubConnection.DisposeAsync();
        }
    }
}