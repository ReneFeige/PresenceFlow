using Microsoft.EntityFrameworkCore;
using PresenceFlow.Models;

namespace PresenceFlow.Data
{
    public class PresenceDbContext : DbContext
    {
        public PresenceDbContext(DbContextOptions<PresenceDbContext> options) : base(options) { }

        public DbSet<Person> People => Set<Person>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>()
                .HasIndex(p => p.Email)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
