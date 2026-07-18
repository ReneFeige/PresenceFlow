using PresenceFlow.Models;

namespace IoBroker_WebApp.Models
{
    public class PeopleData
    {
        public DateTime Timestamp { get; set; }
        public string Version { get; set; } = string.Empty;
        public int PresentCount { get; set; }
        public List<Person> People { get; set; } = new List<Person>();
    }
}
