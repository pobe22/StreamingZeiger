using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StreamingZeiger.Models;
using System.Text.Json;

namespace StreamingZeiger.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<Series> Series { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Listen und Dictionary als JSON speichern
            var stringListConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            var dictionaryConverter = new ValueConverter<Dictionary<string, bool>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, bool>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, bool>());

            modelBuilder.Entity<Movie>()
                .Property(m => m.Genres)
                .HasConversion(stringListConverter);

            modelBuilder.Entity<Movie>()
                .Property(m => m.Cast)
                .HasConversion(stringListConverter);

            modelBuilder.Entity<Movie>()
                .Property(m => m.AvailabilityByService)
                .HasConversion(dictionaryConverter);
        }
    }
}
