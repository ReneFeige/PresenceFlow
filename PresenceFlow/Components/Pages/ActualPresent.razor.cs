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
        var people = await PresenceService.GetPeopleAsync();

        // Daten anwenden (Filtern & Sortieren)
        ApplyData(people);
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
        _hubConnection.On<IReadOnlyList<Person>>("ReceiveUpdate", people =>
            {
                ApplyData(people);

                // UI aktualisieren
                return InvokeAsync(StateHasChanged);
            });

        // SignalR-Verbindung starten
        await _hubConnection.StartAsync();
    }

    // Wendet die Daten auf lokale Properties an
    private void ApplyData(IReadOnlyList<Person> people)
    {
        PresentPeople = people
            .Where(p => p.Status == PresenceStatus.Present)
            .OrderBy(p => p.Timestamp ?? DateTime.MaxValue)
            .ThenBy(p => p.LastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.FirstName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetDisplayName(Person person)
    {
        var fullName = $"{person.FirstName} {person.LastName}".Trim();

        // Email-Adresse anzeigen, sollte der Name fehlen
        return string.IsNullOrWhiteSpace(fullName)
            ? person.Email
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