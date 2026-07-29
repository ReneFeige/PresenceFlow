using Microsoft.EntityFrameworkCore;
using PresenceFlow.Models;

namespace PresenceFlow.Data
{
    public static class PresenceDbSeeder
    {
        public static async Task SeedAsync(PresenceDbContext context)
        {
            if (await context.People.AnyAsync())
            {
                return;
            }

            var people = new List<Person>
        {
            new()
            {
                FirstName = "Anna",
                LastName = "Schmidt",
                Email = "anna.schmidt@example.com",
                Status = PresenceStatus.Present,
                Timestamp = DateTime.Now,
                IsAdmin = true,
                AuthVersion = 1
            },
            new()
            {
                FirstName = "David",
                LastName = "Weber",
                Email = "david.weber@example.com",
                Status = PresenceStatus.Present,
                Timestamp = DateTime.Now,
                IsAdmin = true,
                AuthVersion = 1
            },
            new()
            {
                FirstName = "Sofia",
                LastName = "Keller",
                Email = "sofia.keller@example.com",
                Status = PresenceStatus.Absent,
                Timestamp = DateTime.Now,
                IsAdmin = false,
                AuthVersion = 1
            },
            new()
            {
                FirstName = "Lukas",
                LastName = "Fischer",
                Email = "lukas.fischer@example.com",
                Status = PresenceStatus.Present,
                Timestamp = DateTime.Now,
                IsAdmin = false,
                AuthVersion = 1
            },
            new()
            {
                FirstName = "Marie",
                LastName = "Hoffmann",
                Email = "marie.hoffmann@example.com",
                Status = PresenceStatus.Present,
                Timestamp = DateTime.Now,
                IsAdmin = false,
                AuthVersion = 1
            }
        };

            context.People.AddRange(people);

            await context.SaveChangesAsync();
        }
    }
}
