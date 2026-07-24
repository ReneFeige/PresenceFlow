namespace PresenceFlow.DataAccessLayer
{
    using IoBroker_WebApp.Models;
    using PresenceFlow.Models;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class IoBrokerRepository : IPresenceRepository
    {
        // HttpClient zum Senden von API-Requests
        private readonly HttpClient _httpClient;
        private readonly string _personenUrl;

        // Konstruktor: HttpClient und URLs aus der Konfiguration laden
        public IoBrokerRepository(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;

            _personenUrl = config["IoBroker:PersonenObjectUrl"]
                ?? throw new InvalidOperationException("PersonenObjectUrl fehlt");
        }

        public async Task<IReadOnlyList<Person>> GetPeopleAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_personenUrl);

                if (!response.IsSuccessStatusCode)
                {
                    // HTTP erreichbar, aber Anfrage fehlgeschlagen
                    return [];
                }

                var jsonElement = await response.Content.ReadFromJsonAsync<JsonElement>();

                // 'val' existiert nicht oder hat unerwartete Struktur
                if (!jsonElement.TryGetProperty("val", out var valElement))
                {
                    return [];
                }

                // Zugriff auf die 'val'-Eigenschaft (value) von ioBroker
                var peopleData = valElement.Deserialize<PeopleData>();

                return peopleData?.People ?? [];
            }
            catch (HttpRequestException)
            {
                return [];
            }
            catch (JsonException)
            {
                return [];
            }
            catch (NotSupportedException)
            {
                return [];
            }
        }

        public async Task<Person?> GetPersonAsync(string email)
        {
            var people = await GetPeopleAsync();

            return people.SingleOrDefault(p =>
                p.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        // Setzt den Präsenzstatus einer Person und aktualisiert ioBroker
        public async Task<bool> SetPresenceAsync(string email, PresenceStatus status)
        {
            // Aktuelle Personen-Daten holen
            var people = await GetPeopleAsync();

            // Person anhand der E-Mail finden
            var person = people.SingleOrDefault(p => p.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
          
            if (person == null)
            {
                return false;
            }

            // Prüfen, ob Status bereits korrekt ist
            if (person.Status == status)
            {
                return true;
            }

            // Status ändern
            person.UpdateStatus(status);

            return await SavePeopleAsync(people);
        }

        public async Task<bool> UpdateAuthVersionAsync(string email, int authVersion)
        {
            var people = await GetPeopleAsync();

            var person = people.SingleOrDefault(p => p.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (person == null)
            {
                return false;
            }

            if (person.AuthVersion == authVersion)
            {
                return true;
            }

            person.AuthVersion = authVersion;

            return await SavePeopleAsync(people);
        }

        private async Task<bool> SavePeopleAsync(IReadOnlyList<Person> people)
        {
            var now = DateTime.Now;

            var peopleData = new PeopleData
            {
                Timestamp = now,
                Version = now.Ticks.ToString(),
                PresentCount = people.Count(p => p.Status == PresenceStatus.Present),
                People = people.ToList()
            };

            var payload = new
            {
                val = peopleData,
                ack = false
            };

            var response = await _httpClient.PutAsJsonAsync(_personenUrl, payload);

            return response.IsSuccessStatusCode;
        }
    }
}
