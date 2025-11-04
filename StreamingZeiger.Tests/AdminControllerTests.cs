using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StreamingZeiger.Controllers;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using StreamingZeiger.Services;
using StreamingZeiger.ViewModels;
using Xunit;

namespace StreamingZeiger.Tests
{
    public class AdminControllerTests
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseBackupService> _logger;
        private readonly Mock<IWebHostEnvironment> _env;


        public AdminControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _logger = Mock.Of<ILogger<DatabaseBackupService>>();

            if (!_context.Movies.Any())
            {
                _context.Movies.Add(new Movie
                {
                    Id = 1001,
                    Title = "Test Movie",
                    Year = 2023,
                    DurationMinutes = 120,
                    MediaGenres = new List<MediaGenre>(),
                    Director = "Regisseur",
                    Description = "Beschreibung",
                    Cast = new List<string> { "Schauspieler1" }
                });
            }

            if (!_context.Series.Any())
            {
                var season = new Season
                {
                    SeasonNumber = 1,
                    Episodes = new List<Episode>
                    {
                        new Episode { EpisodeNumber = 1, Title = "Pilot", DurationMinutes = 45 },
                        new Episode { EpisodeNumber = 2, Title = "Episode 2", DurationMinutes = 50 }
                    }
                };

                _context.Series.Add(new Series
                {
                    Id = 2001,
                    Title = "Test Serie",
                    StartYear = 2020,
                    Seasons = new List<Season> { season },
                    MediaGenres = new List<MediaGenre>(),
                    Director = "Regisseur",
                    Description = "Beschreibung",
                    Cast = new List<string> { "Schauspieler A" }
                });
            }

            _context.SaveChanges();

            _env = new Mock<IWebHostEnvironment>();
            _env.Setup(e => e.WebRootPath).Returns(Directory.GetCurrentDirectory());
        }

        private static Mock<UserManager<ApplicationUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mgr = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null, 
                null,
                new List<IUserValidator<ApplicationUser>>(),
                new List<IPasswordValidator<ApplicationUser>>(),
                null,
                new IdentityErrorDescriber(),
                null,
                new Mock<ILogger<UserManager<ApplicationUser>>>().Object
            );

            return mgr;
        }

        private class FakeTmdbService : ITmdbService
        {
            public Task<Movie?> GetMovieByIdAsync(int id, string region)
            {
                // IDs > 500000 oder explizit bekannte "ungültige" IDs simulieren Fehler
                if (id > 500000 || id == 12345678 || id == 87654321 || id == 9999)
                    return Task.FromResult<Movie?>(null);

                return Task.FromResult<Movie?>(new Movie
                {
                    Title = $"MockMovie {id}",
                    TmdbId = id,
                    MediaGenres = new List<MediaGenre>(),
                    Cast = new List<string> { "Actor A" }
                });
            }

            public Task<Series?> GetSeriesByIdAsync(int id, string region)
            {
                if (id > 500000 || id == 12345678 || id == 87654321 || id == 9999)
                    return Task.FromResult<Series?>(null);

                return Task.FromResult<Series?>(new Series
                {
                    Title = $"MockSeries {id}",
                    TmdbId = id,
                    MediaGenres = new List<MediaGenre>(),
                    Cast = new List<string> { "Actor A" },
                    Seasons = new List<Season>
            {
                new Season
                {
                    SeasonNumber = 1,
                    Episodes = new List<Episode>
                    {
                        new Episode { EpisodeNumber = 1, Title = "Pilot", DurationMinutes = 45 }
                    }
                }
            }
                });
            }

            public Task<int?> SearchMovieIdByTitleAsync(string title, string region)
                => Task.FromResult<int?>(1);

            public Task<int?> SearchSeriesIdByTitleAsync(string title, string region)
                => Task.FromResult<int?>(1);

            public Task<List<int>> GetTopMoviesAsync(string region)
            {
                // Simuliere Top-IDs
                var topIds = Enumerable.Range(1, 25).ToList();
                return Task.FromResult(topIds);
            }

            public Task<List<int>> GetTopSeriesAsync(string region)
            {
                var topIds = Enumerable.Range(101, 25).ToList();
                return Task.FromResult(topIds);
            }
        }

        private AdminController GetController()
        {
            var mockUserManager = GetMockUserManager();

            var tmdb = new FakeTmdbService();
            var loggingService = new LoggingService(_context);
            var backupService = new DatabaseBackupService(_context, _logger);

            var controller = new AdminController(_context, _env.Object, null!, tmdb, loggingService, backupService);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            return controller;
        }

        [Fact]
        public async Task Index_ReturnsViewWithMoviesAndSeries()
        {
            var controller = GetController();
            var result = await controller.Index() as ViewResult;
            var model = result.Model as AdminIndexViewModel;

            Assert.NotNull(result);
            Assert.NotNull(model);
            Assert.NotEmpty(model.Movies);
            Assert.NotEmpty(model.Series);
        }

        // ---------------- Movies ----------------
        [Fact]
        public void CreateMovie_Get_ReturnsView()
        {
            var controller = GetController();
            var result = controller.CreateMovie() as ViewResult;
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateMovie_Post_ValidMovie_AddsMovie()
        {
            var controller = GetController();
            var movie = new Movie { Title = "New Movie" };
            var services = new List<string> { "Netflix" };

            var result = await controller.CreateMovie(movie, "Actor1, Actor2", services, "Action, Drama", null)
                as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);

            var createdMovie = _context.Movies
                .Include(m => m.MediaGenres)
                .ThenInclude(mg => mg.Genre)
                .FirstOrDefault(m => m.Title == "New Movie");

            Assert.NotNull(createdMovie);
            var genreNames = createdMovie.MediaGenres.Select(mg => mg.Genre.Name).ToList();
            Assert.Contains("Action", genreNames);
            Assert.Contains("Drama", genreNames);

            Assert.Equal("Film erfolgreich hinzugefügt.", controller.TempData["Message"]);
        }

        [Fact]
        public async Task EditMovie_Get_ReturnsView_WhenMovieExists()
        {
            var controller = GetController();
            var result = await controller.EditMovie(1001) as ViewResult;
            Assert.NotNull(result);
            var movie = result.Model as Movie;
            Assert.Equal(1001, movie.Id);
        }

        [Fact]
        public async Task EditMovie_Post_ValidMovie_UpdatesMovie()
        {
            var controller = GetController();
            var movie = _context.Movies.First(m => m.Id == 1001);
            movie.Title = "Updated Title";
            var services = new List<string> { "Amazon Prime" };
            var result = await controller.EditMovie(1001, movie, "Actor1, Actor2", services, "Comedy", null)
                as RedirectToActionResult;
            Assert.Equal("Index", result.ActionName);
            var updatedMovie = _context.Movies
                .Include(m => m.MediaGenres)
                .ThenInclude(mg => mg.Genre)
                .FirstOrDefault(m => m.Id == 1001);
            Assert.NotNull(updatedMovie);
            Assert.Equal("Updated Title", updatedMovie.Title);
            var genreNames = updatedMovie.MediaGenres.Select(mg => mg.Genre.Name).ToList();
            Assert.Contains("Comedy", genreNames);
        }

        [Fact]
        public async Task DeleteMovie_RemovesMovie()
        {
            var controller = GetController();
            var movieToDelete = _context.Movies.First();
            var result = await controller.DeleteMovie(movieToDelete.Id) as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);
            Assert.DoesNotContain(_context.Movies, m => m.Id == movieToDelete.Id);
        }

        // ---------------- Series ----------------
        [Fact]
        public void CreateSeries_Get_ReturnsView()
        {
            var controller = GetController();
            var result = controller.CreateSeries() as ViewResult;
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateSeries_Post_ValidSeries_AddsSeriesWithSeasons()
        {
            var controller = GetController();
            var series = new Series { Title = "New Series", StartYear = 2022, EndYear = 2023 };
            var services = new List<string> { "Netflix" };

            var result = await controller.CreateSeries(series, "Actor A, Actor B", services, "Drama, Fantasy", null)
                as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);

            var createdSeries = _context.Series
                .Include(s => s.MediaGenres)
                    .ThenInclude(mg => mg.Genre)
                .Include(s => s.Seasons)
                    .ThenInclude(se => se.Episodes)
                .FirstOrDefault(s => s.Title == "New Series");

            Assert.NotNull(createdSeries);
            Assert.Equal("Serie erfolgreich hinzugefügt.", controller.TempData["Message"]);
        }

        [Fact]
        public async Task EditSeries_Get_ReturnsView_WhenSeriesExists()
        {
            var controller = GetController();
            var result = await controller.EditSeries(2001) as ViewResult;

            Assert.NotNull(result);
            var series = result.Model as Series;
            Assert.Equal(2001, series.Id);
        }

        [Fact]
        public async Task EditSeries_Post_ValidSeries_UpdatesSeries()
        {
            var controller = GetController();
            var series = _context.Series.First(s => s.Id == 2001);
            series.Title = "Updated Series Title";
            var services = new List<string> { "Amazon Prime" };
            var result = await controller.EditSeries(2001, series, "Actor X, Actor Y", services, "Thriller")
                as RedirectToActionResult;
            Assert.Equal("Index", result.ActionName);
            var updatedSeries = _context.Series
                .Include(s => s.MediaGenres)
                    .ThenInclude(mg => mg.Genre)
                .FirstOrDefault(s => s.Id == 2001);
            Assert.NotNull(updatedSeries);
            Assert.Equal("Updated Series Title", updatedSeries.Title);
            var genreNames = updatedSeries.MediaGenres.Select(mg => mg.Genre.Name).ToList();
            Assert.Contains("Thriller", genreNames);
        }

        [Fact]
        public async Task DeleteSeries_RemovesSeriesWithSeasonsAndEpisodes()
        {
            var controller = GetController();
            var seriesToDelete = _context.Series
                .Include(s => s.Seasons)
                    .ThenInclude(se => se.Episodes)
                .First();

            var result = await controller.DeleteSeries(seriesToDelete.Id) as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);
            Assert.DoesNotContain(_context.Series, s => s.Id == seriesToDelete.Id);
        }

        // ---------------- Import ----------------
        [Fact]
        public async Task ImportMultiple_Post_ValidIds_ImportsMoviesAndSeries()
        {
            var controller = GetController();

            // IDs vorbereiten
            var tmdbIds = "101,202";
            var typeMovie = "movie";
            var typeSeries = "series";

            // Movie Import
            var resultMovie = await controller.ImportMultiple(tmdbIds, typeMovie, null, null, false, "US") as RedirectToActionResult;
            Assert.Equal("Index", resultMovie.ActionName);
            Assert.Contains(_context.Movies, m => m.TmdbId == 101 || m.TmdbId == 202);

            // Series Import
            var resultSeries = await controller.ImportMultiple(tmdbIds, typeSeries, null, null, false, "US") as RedirectToActionResult;
            Assert.Equal("Index", resultSeries.ActionName);
            Assert.Contains(_context.Series, s => s.TmdbId == 101 || s.TmdbId == 202);

            Assert.NotNull(controller.TempData["Message"]);
        }

        [Fact]
        public async Task ImportMultipleAjax_Post_ValidIds_ImportsMoviesAndWritesProgress()
        {
            var controller = GetController();

            var tmdbIds = "101,202";
            var typeMovie = "movie";

            var httpContext = new DefaultHttpContext();
            var stream = new MemoryStream();
            httpContext.Response.Body = stream;
            controller.ControllerContext.HttpContext = httpContext;

            await controller.ImportMultipleAjax(tmdbIds, null, typeMovie);

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var output = await reader.ReadToEndAsync();

            Assert.Contains("PROGRESS:1/2", output);
            Assert.Contains("PROGRESS:2/2", output);

            Assert.Contains(_context.Movies, m => m.TmdbId == 101);
            Assert.Contains(_context.Movies, m => m.TmdbId == 202);
        }

        [Fact]
        public async Task ImportMultiple_Post_InvalidIds_SetsTempDataMessage()
        {
            var controller = GetController();

            var tmdbIds = "12345645578,876543456321"; 
            var result = await controller.ImportMultiple(tmdbIds, "movie", null, null, false, "US") as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Keine gültigen TMDB-IDs oder Titel gefunden.", controller.TempData["Message"]);
        }

        [Fact]
        public async Task ImportMultipleAjax_Post_InvalidIds_WritesNotFound()
        {
            var controller = GetController();

            var tmdbIds = "994356499"; // Nicht existierende ID
            var httpContext = new DefaultHttpContext();
            var stream = new MemoryStream();
            httpContext.Response.Body = stream;
            controller.ControllerContext.HttpContext = httpContext;

            await controller.ImportMultipleAjax(tmdbIds, null, "movie");

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var output = await reader.ReadToEndAsync();

            Assert.DoesNotContain("/", output); 
            Assert.Equal("", output.Trim());
        }


        // ---------------- ModelState ----------------
        [Fact]
        public async Task CreateMovie_Post_InvalidModel_ReturnsView()
        {
            var controller = GetController();
            controller.ModelState.AddModelError("Title", "Required");

            var movie = new Movie();
            var result = await controller.CreateMovie(movie, "", new List<string>(), "", null) as ViewResult;

            Assert.NotNull(result);
            Assert.Equal(movie, result.Model);
        }
    }
}