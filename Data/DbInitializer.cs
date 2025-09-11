using StreamingZeiger.Models;

namespace StreamingZeiger.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            if (context.Series.Any()) return; // Schon Daten vorhanden

            context.Series.AddRange(
                new Series { Title = "Breaking Bad", Description = "Chemielehrer wird Drogenboss", Genre = "Drama", Seasons = 5, Episodes = 62, PosterUrl = "/images/breakingbad.jpg" },
                new Series { Title = "Stranger Things", Description = "Mystery in Hawkins", Genre = "Sci-Fi", Seasons = 4, Episodes = 34, PosterUrl = "/images/strangerthings.jpg" }
            );

            context.SaveChanges();
        }
    }
}
