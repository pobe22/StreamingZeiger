using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StreamingZeiger.Controllers;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace StreamingZeiger.Tests
{
    public class SeriesControllerTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly SeriesController _controller;

        public SeriesControllerTests()
        {
            // InMemory DB
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // Beispiel-Daten
            var genre = new Genre { Id = 1, Name = "Drama" };
            var series1 = new Series
            {
                Id = 1,
                Title = "Series1",
                StartYear = 2020,
                Rating = 8.0,
                MediaGenres = new List<MediaGenre> { new MediaGenre { GenreId = 1, Genre = genre } },
                Seasons = new List<Season>
                {
                    new Season
                    {
                        Id = 1, SeasonNumber = 1, SeriesId = 1,
                        Episodes = new List<Episode>
                        {
                            new Episode { Id = 1, Title = "Pilot", SeasonId = 1 }
                        }
                    }
                }
            };

            var series2 = new Series
            {
                Id = 2,
                Title = "Series2",
                StartYear = 2021,
                Rating = 7.5,
                MediaGenres = new List<MediaGenre> { new MediaGenre { GenreId = 1, Genre = genre } }
            };

            _context.Genres.Add(genre);
            _context.Series.AddRange(series1, series2);
            _context.SaveChanges();

            // Mock UserManager
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null
            );

            // Controller
            _controller = new SeriesController(_context, _userManagerMock.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithSeries()
        {
            var result = await _controller.Index() as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Series>>(result.Model);
            Assert.Equal(2, model.Count());
        }

        [Fact]
        public async Task Details_ReturnsViewResult_WithSeriesAndRecommendations()
        {
            // Arrange: Mock User
            var user = new ApplicationUser { Id = "user1" };

            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

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
            var series = Assert.IsType<Series>(result.Model);
            Assert.Equal(1, series.Id);
            Assert.NotNull(_controller.ViewBag.RecommendedSeries);
            Assert.NotNull(_controller.ViewBag.InWatchlist);
        }
    }
}
