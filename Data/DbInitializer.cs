using StreamingZeiger.Models;

namespace StreamingZeiger.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // Serien
            if (!context.Series.Any())
            {
                context.Series.AddRange(
                    new Series
                    {
                        Title = "Breaking Bad",
                        Description = "Chemielehrer wird Drogenboss",
                        Genre = "Drama",
                        Seasons = 5,
                        Episodes = 62,
                        PosterUrl = "/images/breakingbad.jpg"
                    },
                    new Series
                    {
                        Title = "Stranger Things",
                        Description = "Mystery in Hawkins",
                        Genre = "Sci-Fi",
                        Seasons = 4,
                        Episodes = 34,
                        PosterUrl = "/images/strangerthings.jpg"
                    },
                    new Series
                    {
                        Title = "Game of Thrones",
                        Description = "Kampf um den Eisernen Thron",
                        Genre = "Fantasy",
                        Seasons = 8,
                        Episodes = 73,
                        PosterUrl = "/images/got.jpg"
                    }
                );
            }

            // Filme
            if (!context.Movies.Any())
            {
                context.Movies.AddRange(
                    new Movie
                    {
                        Title = "Inception",
                        OriginalTitle = "Inception",
                        Genres = new List<string> { "Sci-Fi", "Thriller" },
                        Year = 2010,
                        DurationMinutes = 148,
                        Description = "Ein Dieb dringt in die Träume anderer Menschen ein, um ihre Geheimnisse zu stehlen.",
                        Cast = new List<string> { "Leonardo DiCaprio", "Joseph Gordon-Levitt", "Elliot Page" },
                        Director = "Christopher Nolan",
                        PosterFile = "/images/posters/inception.jpg",
                        TrailerUrl = "https://www.youtube.com/embed/YoHD9XEInc0",
                        AvailabilityByService = new Dictionary<string, bool> { { "Netflix", true }, { "Prime Video", false }, { "Disney+", true } },
                        Rating = 8.8
                    },
                    new Movie
                    {
                        Title = "The Matrix",
                        OriginalTitle = "The Matrix",
                        Genres = new List<string> { "Sci-Fi", "Action" },
                        Year = 1999,
                        DurationMinutes = 136,
                        Description = "Ein Hacker entdeckt, dass die Welt eine Simulation ist.",
                        Cast = new List<string> { "Keanu Reeves", "Laurence Fishburne", "Carrie-Anne Moss" },
                        Director = "Lana Wachowski, Lilly Wachowski",
                        PosterFile = "/images/posters/matrix.jpg",
                        TrailerUrl = "https://www.youtube.com/embed/vKQi3bBA1y8",
                        AvailabilityByService = new Dictionary<string, bool> { { "Netflix", false }, { "Prime Video", true }, { "Disney+", false } },
                        Rating = 8.7
                    },
                    new Movie
                    {
                        Title = "Interstellar",
                        OriginalTitle = "Interstellar",
                        Genres = new List<string> { "Sci-Fi", "Drama" },
                        Year = 2014,
                        DurationMinutes = 169,
                        Description = "Eine Gruppe von Astronauten reist durch ein Wurmloch auf der Suche nach einer neuen Heimat für die Menschheit.",
                        Cast = new List<string> { "Matthew McConaughey", "Anne Hathaway", "Jessica Chastain" },
                        Director = "Christopher Nolan",
                        PosterFile = "/images/posters/interstellar.jpg",
                        TrailerUrl = "https://www.youtube.com/embed/zSWdZVtXT7E",
                        AvailabilityByService = new Dictionary<string, bool> { { "Netflix", true }, { "Prime Video", true }, { "Disney+", false } },
                        Rating = 8.6
                    }
                );
            }

            context.SaveChanges();
        }
    }
}
