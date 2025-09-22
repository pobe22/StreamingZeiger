using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using StreamingZeiger.Controllers;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using StreamingZeiger.ViewModels;

namespace StreamingZeiger.Tests
{
    public class AdminControllerTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IWebHostEnvironment> _env;

        public AdminControllerTests()
        {
            // InMemory DbContext erstellen
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);

            // Test-Filme anlegen
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

            // Test-Serien anlegen
            if (!_context.Series.Any())
            {
                _context.Series.Add(new Series
                {
                    Id = 2001,
                    Title = "Test Serie",
                    StartYear = 2020,
                    Seasons = 1,
                    Episodes = 10,
                    Director = "Regisseur",
                    Description = "Beschreibung",
                    Cast = new List<string> { "Schauspieler A" }
                });
            }

            _context.SaveChanges();

            // Mocked Environment
            _env = new Mock<IWebHostEnvironment>();
            _env.Setup(e => e.WebRootPath).Returns(Directory.GetCurrentDirectory());
        }

        // Controller mit TempData erstellen
        private AdminController GetController()
        {
            var controller = new AdminController(_context, _env.Object);

            // TempData für Tests initialisieren
            controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>()
            );

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

            // Prüfen, ob der Film existiert
            var createdMovie = _context.Movies
                .Include(m => m.MediaGenres)   // EF Core Navigation laden
                .ThenInclude(mg => mg.Genre)
                .FirstOrDefault(m => m.Title == "New Movie");

            Assert.NotNull(createdMovie);

            // Prüfen, ob die Genres korrekt zugewiesen wurden
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

            var movie = result.Model as MediaItem; // statt Movie
            Assert.Equal(1001, movie.Id);
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

        [Fact]
        public async Task ImportFromTmdb_ReturnsJsonResult()
        {
            var controller = GetController();

            // Für einen Film
            var resultMovie = await controller.ImportFromTmdb(550, "movie") as JsonResult;
            Assert.NotNull(resultMovie);
            Assert.NotNull(resultMovie.Value);

            // Für eine Serie
            var resultSeries = await controller.ImportFromTmdb(12345, "series") as JsonResult;
            Assert.NotNull(resultSeries);
            Assert.NotNull(resultSeries.Value);
        }

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
