using System.Text.Json.Serialization;

namespace PresenceFlow.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public PresenceStatus Status { get; set; }
        public DateTime? Timestamp { get; set; }
        public bool IsAdmin { get; set; } = false;
        public int AuthVersion { get; set; } = 1;

        /// <summary>
        /// Ändert den Anwesenheitsstatus inkl. Zeitstempel
        /// </summary>
        public void UpdateStatus(PresenceStatus newStatus)
        {
            Status = newStatus;
            Timestamp = DateTime.Now;
        }
    }

    // schreibt und liest anwesend / abwesend als string in ioBroker
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PresenceStatus
    {
        Present,
        Absent
    }
}
