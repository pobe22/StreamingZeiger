using Microsoft.AspNetCore.Identity;
using StreamingZeiger.Models;
using Microsoft.EntityFrameworkCore;

namespace StreamingZeiger.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
             AppDbContext context,
             UserManager<ApplicationUser> userManager,
             RoleManager<IdentityRole> roleManager)
        {
            await context.Database.EnsureCreatedAsync();

            // --- Admin-Rolle und Benutzer ---
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var adminUser = await userManager.FindByEmailAsync("admin@streamingzeiger.at");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@streamingzeiger.at",
                    Email = "admin@streamingzeiger.at",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(adminUser, "Admin123!");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // --- Genres vorbereiten ---
            if (!await context.Genres.AnyAsync())
            {
                var drama = new Genre { Name = "Drama" };
                var sciFi = new Genre { Name = "Sci-Fi" };
                var fantasy = new Genre { Name = "Fantasy" };
                var thriller = new Genre { Name = "Thriller" };
                var action = new Genre { Name = "Action" };

                context.Genres.AddRange(drama, sciFi, fantasy, thriller, action);
                await context.SaveChangesAsync();
            }

            var genresList = await context.Genres.ToListAsync();
            var genres = genresList
                .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToDictionary(g => g.Name, g => g, StringComparer.OrdinalIgnoreCase);


            // --- Serien ---
            if (!await context.Series.AnyAsync())
            {
                var breakingBad = new Series
                {
                    Title = "Breaking Bad",
                    OriginalTitle = "Breaking Bad",
                    StartYear = 2008,
                    EndYear = 2013,
                    Description = "Chemielehrer wird Drogenboss",
                    Cast = new List<string> { "Bryan Cranston", "Aaron Paul", "Anna Gunn" },
                    Director = "Vince Gilligan",
                    PosterFile = "/images/posters/breakingbad.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/HhesaQXLuRY",
                    Rating = 9.5,
                    MediaGenres = new List<MediaGenre>
        {
            new MediaGenre { Genre = genres["Drama"] }
        },
                    Seasons = new List<Season>
        {
            new Season
            {
                SeasonNumber = 1,
                Episodes = new List<Episode>
                {
                    new Episode { EpisodeNumber = 1, Title = "Pilot", DurationMinutes = 58 },
                    new Episode { EpisodeNumber = 2, Title = "Die Katze ist im Sack", DurationMinutes = 48 }
                }
            },
            new Season
            {
                SeasonNumber = 2,
                Episodes = new List<Episode>
                {
                    new Episode { EpisodeNumber = 1, Title = "Sieben Dreißig Sieben", DurationMinutes = 47 }
                }
            }
        }
                };

                var strangerThings = new Series
                {
                    Title = "Stranger Things",
                    OriginalTitle = "Stranger Things",
                    StartYear = 2016,
                    Description = "Mystery in Hawkins",
                    Cast = new List<string> { "Millie Bobby Brown", "Finn Wolfhard", "Winona Ryder" },
                    Director = "The Duffer Brothers",
                    PosterFile = "/images/posters/strangerthings.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/mnd7sFt5c3A",
                    Rating = 8.8,
                    MediaGenres = new List<MediaGenre>
        {
            new MediaGenre { Genre = genres["Sci-Fi"] }
        },
                    Seasons = new List<Season>
        {
            new Season
            {
                SeasonNumber = 1,
                Episodes = new List<Episode>
                {
                    new Episode { EpisodeNumber = 1, Title = "Kapitel Eins: Das Verschwinden des Will Byers", DurationMinutes = 47 },
                    new Episode { EpisodeNumber = 2, Title = "Kapitel Zwei: Die seltsame Verrückte", DurationMinutes = 55 }
                }
            }
        }
                };

                var gameOfThrones = new Series
                {
                    Title = "Game of Thrones",
                    OriginalTitle = "Game of Thrones",
                    StartYear = 2011,
                    EndYear = 2019,
                    Description = "Kampf um den Eisernen Thron",
                    Cast = new List<string> { "Emilia Clarke", "Kit Harington", "Peter Dinklage" },
                    Director = "David Benioff & D.B. Weiss",
                    PosterFile = "/images/posters/got.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/BpJYNVhGf1s",
                    Rating = 9.3,
                    MediaGenres = new List<MediaGenre>
    {
        new MediaGenre { Genre = genres["Fantasy"] }
    },
                    Seasons = new List<Season>
    {
        new Season
        {
            SeasonNumber = 1,
            Episodes = new List<Episode>
            {
                new Episode { EpisodeNumber = 1, Title = "Winter Is Coming", DurationMinutes = 62 },
                new Episode { EpisodeNumber = 2, Title = "The Kingsroad", DurationMinutes = 56 }
            }
        },
        new Season
        {
            SeasonNumber = 2,
            Episodes = new List<Episode>
            {
                new Episode { EpisodeNumber = 1, Title = "The North Remembers", DurationMinutes = 53 },
                new Episode { EpisodeNumber = 2, Title = "The Night Lands", DurationMinutes = 54 }
            }
        },
    }
                };


                context.Series.AddRange(breakingBad, strangerThings, gameOfThrones);
                await context.SaveChangesAsync();
            }

            // --- Filme ---
            if (!await context.Movies.AnyAsync())
            {
                var inception = new Movie
                {
                    Title = "Inception",
                    OriginalTitle = "Inception",
                    Year = 2010,
                    DurationMinutes = 148,
                    Description = "Ein Dieb dringt in die Träume anderer Menschen ein...",
                    Cast = new List<string> { "Leonardo DiCaprio", "Joseph Gordon-Levitt", "Elliot Page" },
                    Director = "Christopher Nolan",
                    PosterFile = "/images/posters/inception.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/YoHD9XEInc0",
                    AvailabilityByService = new Dictionary<string, bool>
                    {
                        { "Netflix", true },
                        { "Prime Video", false },
                        { "Disney+", true }
                    },
                    Rating = 8.8,
                    MediaGenres = new List<MediaGenre>
                    {
                        new MediaGenre { Genre = genres["Sci-Fi"] },
                        new MediaGenre { Genre = genres["Thriller"] }
                    }
                };

                var matrix = new Movie
                {
                    Title = "The Matrix",
                    OriginalTitle = "The Matrix",
                    Year = 1999,
                    DurationMinutes = 136,
                    Description = "Ein Hacker entdeckt, dass die Welt eine Simulation ist.",
                    Cast = new List<string> { "Keanu Reeves", "Laurence Fishburne", "Carrie-Anne Moss" },
                    Director = "Lana Wachowski, Lilly Wachowski",
                    PosterFile = "/images/posters/matrix.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/vKQi3bBA1y8",
                    AvailabilityByService = new Dictionary<string, bool>
                    {
                        { "Netflix", false },
                        { "Prime Video", true },
                        { "Disney+", false }
                    },
                    Rating = 8.7,
                    MediaGenres = new List<MediaGenre>
                    {
                        new MediaGenre { Genre = genres["Sci-Fi"] },
                        new MediaGenre { Genre = genres["Action"] }
                    }
                };

                var interstellar = new Movie
                {
                    Title = "Interstellar",
                    OriginalTitle = "Interstellar",
                    Year = 2014,
                    DurationMinutes = 169,
                    Description = "Eine Gruppe von Astronauten reist durch ein Wurmloch...",
                    Cast = new List<string> { "Matthew McConaughey", "Anne Hathaway", "Jessica Chastain" },
                    Director = "Christopher Nolan",
                    PosterFile = "/images/posters/interstellar.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/zSWdZVtXT7E",
                    AvailabilityByService = new Dictionary<string, bool>
                    {
                        { "Netflix", true },
                        { "Prime Video", true },
                        { "Disney+", false }
                    },
                    Rating = 8.6,
                    MediaGenres = new List<MediaGenre>
                    {
                        new MediaGenre { Genre = genres["Sci-Fi"] },
                        new MediaGenre { Genre = genres["Drama"] }
                    }
                };

                context.Movies.AddRange(inception, matrix, interstellar);
                await context.SaveChangesAsync();
            }
        }
    }
}