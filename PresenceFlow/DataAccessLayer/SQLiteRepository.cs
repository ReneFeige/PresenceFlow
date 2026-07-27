using Microsoft.EntityFrameworkCore;
using PresenceFlow.Data;
using PresenceFlow.Models;

namespace PresenceFlow.DataAccessLayer
{
    public class SQLiteRepository : IPresenceRepository
    {
        private readonly PresenceDbContext _context;

        public SQLiteRepository(PresenceDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Person>> GetPeopleAsync()
        {
            return await _context.People
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .ToListAsync();
        }

        public async Task<Person?> GetPersonAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            return await _context.People
                .SingleOrDefaultAsync(p => p.Email == normalizedEmail);
        }

        public async Task<bool> SetPresenceAsync(string email, PresenceStatus status)
        {
            var person = await GetPersonAsync(email);

            if (person == null)
            {
                return false;
            }

            if (person.Status == status)
            {
                return true;
            }

            person.UpdateStatus(status);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateAuthVersionAsync(string email, int authVersion)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            var person = await _context.People
                .SingleOrDefaultAsync(p => p.Email == normalizedEmail);

            if (person == null)
            {
                return false;
            }

            person.AuthVersion = authVersion;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
