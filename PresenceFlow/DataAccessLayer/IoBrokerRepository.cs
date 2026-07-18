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

                // Zugriff auf die 'val'-Eigenschaft (value) von ioBroker
                if (jsonElement.TryGetProperty("val", out var valElement))
                {
                    var peopleData = valElement.Deserialize<PeopleData>();
                    return peopleData?.People ?? [];
                }

                // 'val' existiert nicht oder hat unerwartete Struktur
                return [];
            }
            catch (Exception)
            {
                // Alles weitere, was schiefgehen könnte (Verbindungsfehler, JSON, ...)
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
        public async Task<bool> SetPresenceAsync(string email, PresenceStatus newStatus)
        {
            // Aktuelle Personen-Daten holen
            var peopleData = await GetPeopleAsync();
            if (peopleData == null)
            {
                return false;
            }

            // Person anhand der E-Mail finden
            var person = peopleData.SingleOrDefault(p => p.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (person == null)
            {
                return false;
            }

            // Prüfen, ob Status bereits korrekt ist
            if (person.Status == newStatus)
            {
                return true;
            }

            // Status ändern
            person.UpdateStatus(newStatus);

            // Payload (JSON) für API-Requests vorbereiten
            var payloadPeople = new { val = peopleData };

            // Personen-Objekt im ioBroker aktualisieren
            var responsePeople = await _httpClient.PutAsJsonAsync(_personenUrl, payloadPeople);

            // True zurückgeben, wenn Request erfolgreich war
            return responsePeople.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAuthVersionAsync(PeopleData peopleData)
        {
            // Meta-Daten aktualisieren
            peopleData.Timestamp = DateTime.Now;

            var payload = new
            {
                val = peopleData,
                ack = false
            };

            var response = await _httpClient.PutAsJsonAsync(_personenUrl, payload);

            return response.IsSuccessStatusCode;
        }

        public Task<bool> UpdateAuthVersionAsync(string email, int authVersion)
        {
            throw new NotImplementedException();
        }
    }
}
