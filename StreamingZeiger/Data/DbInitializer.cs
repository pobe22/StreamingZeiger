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

            // Serien initialisieren
            if (!await context.Series.AnyAsync())
            {
                // Genres abrufen oder anlegen
                var drama = await context.Genres.FirstOrDefaultAsync(g => g.Name == "Drama") ?? new Genre { Name = "Drama" };
                var sciFi = await context.Genres.FirstOrDefaultAsync(g => g.Name == "Sci-Fi") ?? new Genre { Name = "Sci-Fi" };
                var fantasy = await context.Genres.FirstOrDefaultAsync(g => g.Name == "Fantasy") ?? new Genre { Name = "Fantasy" };

                context.Genres.AddRange(drama, sciFi, fantasy);

                var breakingBad = new Series
                {
                    Title = "Breaking Bad",
                    OriginalTitle = "Breaking Bad",
                    StartYear = 2008,
                    EndYear = 2013,
                    Seasons = 5,
                    Episodes = 62,
                    Description = "Chemielehrer wird Drogenboss",
                    Cast = new List<string> { "Bryan Cranston", "Aaron Paul", "Anna Gunn" },
                    Director = "Vince Gilligan",
                    PosterFile = "/images/posters/breakingbad.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/HhesaQXLuRY",
                    Rating = 9.5,
                    SeriesGenres = new List<SeriesGenre>
        {
            new SeriesGenre { Genre = drama }
        }
                };

                var strangerThings = new Series
                {
                    Title = "Stranger Things",
                    OriginalTitle = "Stranger Things",
                    StartYear = 2016,
                    Seasons = 4,
                    Episodes = 34,
                    Description = "Mystery in Hawkins",
                    Cast = new List<string> { "Millie Bobby Brown", "Finn Wolfhard", "Winona Ryder" },
                    Director = "The Duffer Brothers",
                    PosterFile = "/images/posters/strangerthings.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/mnd7sFt5c3A",
                    Rating = 8.8,
                    SeriesGenres = new List<SeriesGenre>
        {
            new SeriesGenre { Genre = sciFi }
        }
                };

                var gameOfThrones = new Series
                {
                    Title = "Game of Thrones",
                    OriginalTitle = "Game of Thrones",
                    StartYear = 2011,
                    EndYear = 2019,
                    Seasons = 8,
                    Episodes = 73,
                    Description = "Kampf um den Eisernen Thron",
                    Cast = new List<string> { "Emilia Clarke", "Kit Harington", "Peter Dinklage" },
                    Director = "David Benioff & D.B. Weiss",
                    PosterFile = "/images/posters/got.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/BpJYNVhGf1s",
                    Rating = 9.3,
                    SeriesGenres = new List<SeriesGenre>
        {
            new SeriesGenre { Genre = fantasy }
        }
                };

                context.Series.AddRange(breakingBad, strangerThings, gameOfThrones);

                await context.SaveChangesAsync();
            }


            // Filme
            if (!await context.Movies.AnyAsync())
            {
                var sciFi = await context.Genres.FirstOrDefaultAsync(g => g.Name == "Sci-Fi") ?? new Genre { Name = "Sci-Fi" };
                var thriller = await context.Genres.FirstOrDefaultAsync(g => g.Name == "Thriller") ?? new Genre { Name = "Thriller" };
                var action = await context.Genres.FirstOrDefaultAsync(g => g.Name == "Action") ?? new Genre { Name = "Action" };
                var drama = await context.Genres.FirstOrDefaultAsync(g => g.Name == "Drama") ?? new Genre { Name = "Drama" };

                context.Genres.AddRange(sciFi, thriller, action, drama);

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
                    AvailabilityByService = new Dictionary<string, bool> { { "Netflix", true }, { "Prime Video", false }, { "Disney+", true } },
                    Rating = 8.8,
                    MovieGenres = new List<MovieGenre>
                    {
                        new MovieGenre { Genre = sciFi },
                        new MovieGenre { Genre = thriller }
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
                    AvailabilityByService = new Dictionary<string, bool> { { "Netflix", false }, { "Prime Video", true }, { "Disney+", false } },
                    Rating = 8.7,
                    MovieGenres = new List<MovieGenre>
                    {
                        new MovieGenre { Genre = sciFi },
                        new MovieGenre { Genre = action }
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
                    AvailabilityByService = new Dictionary<string, bool> { { "Netflix", true }, { "Prime Video", true }, { "Disney+", false } },
                    Rating = 8.6,
                    MovieGenres = new List<MovieGenre>
                    {
                        new MovieGenre { Genre = sciFi },
                        new MovieGenre { Genre = drama }
                    }
                };

                context.Movies.AddRange(inception, matrix, interstellar);
            }

            await context.SaveChangesAsync();
        }
    }
}
