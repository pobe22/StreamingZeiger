using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StreamingZeiger.Controllers;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using StreamingZeiger.Services;
using StreamingZeiger.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace StreamingZeiger.Tests
{
    public class MoviesControllerTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IStaticMovieRepository> _repoMock;
        private readonly MoviesController _controller;

        public MoviesControllerTests()
        {
            // InMemory DB
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // Beispiel-Daten
            _context.Movies.AddRange(
                new Movie { Id = 1, Title = "Movie1", Year = 2020, Rating = 7.5 },
                new Movie { Id = 2, Title = "Movie2", Year = 2022, Rating = 8.2 },
                new Movie { Id = 3, Title = "AnotherMovie", Year = 2019, Rating = 6.9 }
            );
            _context.SaveChanges();

            // Mock UserManager
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null
            );

            // Mock Repository
            _repoMock = new Mock<IStaticMovieRepository>();

            // Controller
            _controller = new MoviesController(_repoMock.Object, _context, _userManagerMock.Object);
        }

        [Fact]
        public void Index_ReturnsViewResult_WithMovies()
        {
            var filter = new MovieFilterViewModel { Page = 1, PageSize = 10 };

            var result = _controller.Index(filter) as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Movie>>(result.Model);
            Assert.Equal(3, model.Count());
        }

        [Fact]
        public async Task Details_ReturnsViewResult_WithMovieAndRecommendations()
        {
            // Arrange: Mock User
            var user = new ApplicationUser { Id = "user1" };

            // Mock UserManager
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Mock HttpContext mit User
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
        new Claim(ClaimTypes.NameIdentifier, "user1"),
        new Claim(ClaimTypes.Name, "testuser")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Details(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var movie = Assert.IsType<Movie>(result.Model);
            Assert.Equal(1, movie.Id);
            Assert.NotNull(_controller.ViewBag.RecommendedMovies);
            Assert.NotNull(_controller.ViewBag.InWatchlist);
        }

        [Fact]
        public void Autocomplete_ReturnsJsonResult_WithTitles()
        {
            var result = _controller.Autocomplete("Movie") as JsonResult;

            Assert.NotNull(result);
            var titles = Assert.IsAssignableFrom<List<string>>(result.Value);
            Assert.Contains("Movie1", titles);
            Assert.Contains("Movie2", titles);
        }

        [Fact]
        public void TrackShare_RedirectsToDetails()
        {
            var result = _controller.TrackShare(1, "Facebook") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Details", result.ActionName);
            Assert.Equal(1, result.RouteValues["id"]);
        }
    }
}
