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
                Email = "schmidt@flow.de",
                Status = PresenceStatus.Present,
                Timestamp = DateTime.Now,
                IsAdmin = true,
                AuthVersion = 1
            },
            new()
            {
                FirstName = "David",
                LastName = "Weber",
                Email = "weber@flow.de",
                Status = PresenceStatus.Absent,
                Timestamp = DateTime.Now,
                IsAdmin = true,
                AuthVersion = 1
            },
            new()
            {
                FirstName = "Sofia",
                LastName = "Keller",
                Email = "keller@flow.de",
                Status = PresenceStatus.Present,
                Timestamp = DateTime.Now,
                IsAdmin = false,
                AuthVersion = 1
            },
            new()
            {
                FirstName = "Lukas",
                LastName = "Fischer",
                Email = "fischer@flow.de",
                Status = PresenceStatus.Present,
                Timestamp = DateTime.Now,
                IsAdmin = false,
                AuthVersion = 1
            },
            new()
            {
                FirstName = "Marie",
                LastName = "Hoffmann",
                Email = "hoffmann@flow.de",
                Status = PresenceStatus.Present,
                Timestamp = DateTime.Now,
                IsAdmin = false,
                AuthVersion = 1
            },
        };

            context.People.AddRange(people);

            await context.SaveChangesAsync();
        }
    }
}
