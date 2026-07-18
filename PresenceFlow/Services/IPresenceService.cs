using PresenceFlow.Models;

namespace PresenceFlow.Services
{
    public interface IPresenceService
    {
        Task<Person?> GetPersonAsync(string email);
        Task<IReadOnlyList<Person>> GetPeopleAsync();
        Task<IReadOnlyList<Person>> GetPresentPeopleAsync();
        Task<int> GetPresentCountAsync();
        Task<bool> LoginAsync(string email);
        Task<bool> LogoutAsync(string email);
        Task<bool> UpdatePersonAuthVersionAsync(string email, int authVersion);
    }
}
