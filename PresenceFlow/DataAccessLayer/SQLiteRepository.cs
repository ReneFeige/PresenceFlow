using Microsoft.EntityFrameworkCore;
using PresenceFlow.Data;
using PresenceFlow.Models;

namespace PresenceFlow.DataAccessLayer
{
    public class SQLiteRepository : IPresenceRepository
    {
        private readonly IDbContextFactory<PresenceDbContext> _contextFactory;

        public SQLiteRepository(IDbContextFactory<PresenceDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IReadOnlyList<Person>> GetPeopleAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.People
                .AsNoTracking()
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
        }

        public async Task<Person?> GetPersonAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.People
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.Email == normalizedEmail);
        }

        public async Task<bool> SetPresenceAsync(string email, PresenceStatus status)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            await using var context = await _contextFactory.CreateDbContextAsync();

            var person = await context.People.SingleOrDefaultAsync(person => person.Email == normalizedEmail);

            if (person == null)
            {
                return false;
            }

            if (person.Status == status)
            {
                return true;
            }

            person.UpdateStatus(status);

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateAuthVersionAsync(string email, int authVersion)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            await using var context = await _contextFactory.CreateDbContextAsync();

            var person = await context.People.SingleOrDefaultAsync(person => person.Email == normalizedEmail);

            if (person == null)
            {
                return false;
            }

            person.AuthVersion = authVersion;

            await context.SaveChangesAsync();

            return true;
        }
    }
}
