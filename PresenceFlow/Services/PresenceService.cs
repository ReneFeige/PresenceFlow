using Microsoft.AspNetCore.SignalR;
using PresenceFlow.DataAccessLayer;
using PresenceFlow.Hubs;
using PresenceFlow.Models;
using PresenceFlow.Services;

public class PresenceService : IPresenceService
{
    private readonly IPresenceRepository _repository;
    private readonly IHubContext<PresenceHub> _hub;

    // Konstruktor: IPresenceRepository und HubContext injizieren
    public PresenceService(IPresenceRepository repository, IHubContext<PresenceHub> hub)
    {
        _repository = repository;
        _hub = hub;
    }

    // Gibt die Person mit der angegebenen E-Mail zurück, oder null, wenn nicht gefunden
    public Task<Person?> GetPersonAsync(string email)
    {
        return _repository.GetPersonAsync(email);
    }

    // Meldet die Person "anwesend"
    public async Task<bool> LoginAsync(string email)
    {
        var success = await _repository.SetPresenceAsync(email, PresenceStatus.Present);

        // Wenn erfolgreich, alle Clients über SignalR benachrichtigen
        if (success)
        {
            await NotifyClientsAsync();
        }

            return success;
    }

    // Meldet die Person "abwesend"
    public async Task<bool> LogoutAsync(string email)
    {
        var success = await _repository.SetPresenceAsync(email, PresenceStatus.Absent);

        // Wenn erfolgreich, alle Clients über SignalR benachrichtigen
        if (success) 
        {
            await NotifyClientsAsync();
        }

            return success;
    }

    // Gibt alle Personen und deren Status zurück
    public Task<IReadOnlyList<Person>> GetPeopleAsync()
    {
        return _repository.GetPeopleAsync();
    }

    public async Task<IReadOnlyList<Person>> GetPresentPeopleAsync()
    {
        var people = await _repository.GetPeopleAsync();

        return people
            .Where(p => p.Status == PresenceStatus.Present)      // Nur anwesende Personen
            .OrderBy(p => p.Timestamp ?? DateTime.MaxValue)      // Sortieren nach Ankunftszeit
            .ThenBy(person => person.LastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(person => person.FirstName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<int> GetPresentCountAsync()
    {
        var people = await _repository.GetPeopleAsync();

        return people.Count(p => p.Status == PresenceStatus.Present);
    }

    public async Task<bool> UpdatePersonAuthVersionAsync(string email, int newAuthVersion)
    {
        return await _repository.UpdateAuthVersionAsync(email, newAuthVersion);
    }

    // Benachrichtigt alle verbundenen Clients über SignalR, dass sich die Präsenzdaten geändert haben
    private async Task NotifyClientsAsync()
    {
        var people = await _repository.GetPeopleAsync();
        await _hub.Clients.All.SendAsync("ReceiveUpdate", people);
    }
}
