using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StreamingZeiger.Models;
using System.Text.Json;

namespace StreamingZeiger.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // MediaItems + Subtypen
        public DbSet<MediaItem> MediaItems { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Series> Series { get; set; }
        public DbSet<Season> Seasons { get; set; }
        public DbSet<Episode> Episodes { get; set; }

        // Genres
        public DbSet<Genre> Genres { get; set; }
        public DbSet<MediaGenre> MediaGenres { get; set; }

        // User-bezogene Entitäten
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<WatchlistItem> WatchlistItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Vererbung TPH ---
            modelBuilder.Entity<MediaItem>()
                .HasDiscriminator<string>("MediaType")
                .HasValue<Movie>("Movie")
                .HasValue<Series>("Series");

            // --- JSON Konverter ---
            var stringListConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            modelBuilder.Entity<MediaItem>()
                .Property(m => m.Cast)
                .HasConversion(stringListConverter);

            var dictionaryConverter = new ValueConverter<Dictionary<string, bool>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, bool>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, bool>());

            modelBuilder.Entity<MediaItem>()
                .Property(m => m.AvailabilityByService)
                .HasConversion(dictionaryConverter);

            // --- MediaGenre Join ---
            modelBuilder.Entity<MediaGenre>()
                .HasKey(mg => new { mg.MediaItemId, mg.GenreId });

            modelBuilder.Entity<MediaGenre>()
                .HasOne(mg => mg.MediaItem)
                .WithMany(mi => mi.MediaGenres)
                .HasForeignKey(mg => mg.MediaItemId);

            modelBuilder.Entity<MediaGenre>()
                .HasOne(mg => mg.Genre)
                .WithMany(g => g.MediaGenres)
                .HasForeignKey(mg => mg.GenreId);

            modelBuilder.Entity<WatchlistItem>()
                .HasOne(w => w.MediaItem)
                .WithMany(mi => mi.WatchlistItems)
                .HasForeignKey(w => w.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
