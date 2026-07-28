using PresenceFlow.Models;
using System.Security.Claims;

namespace PresenceFlow.Services
{
    public interface IPresenceService
    {
        Task<Person?> GetPersonAsync(string email);
        Task<IReadOnlyList<Person>> GetPeopleAsync();
        Task<IReadOnlyList<Person>> GetPresentPeopleAsync();
        Task<int> GetPresentCountAsync();

        Task<bool> SetPresenceForCurrentUserAsync(ClaimsPrincipal user, PresenceStatus status);
        Task<bool> SetPresenceForTrustedEmailAsync(string email, PresenceStatus status);
        Task<bool> UpdatePersonAuthVersionAsync(string email, int authVersion);
    }
}
