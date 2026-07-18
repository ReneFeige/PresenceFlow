using PresenceFlow.Models;

namespace PresenceFlow.DataAccessLayer
{
    public interface IPresenceRepository
    {
        Task<IReadOnlyList<Person>> GetPeopleAsync();
        Task<Person?> GetPersonAsync(string email);
        Task<bool> SetPresenceAsync(string email, PresenceStatus status);
        Task<bool> UpdateAuthVersionAsync(string email, int authVersion);
    }
}
