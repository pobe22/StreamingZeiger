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
        [Fact]
        public async Task SeasonDetails_ReturnsViewResult_WithSeasonAndEpisodes()
        {
            // Arrange: Get existing season id
            var season = await _context.Seasons.Include(s => s.Episodes).FirstOrDefaultAsync();
            Assert.NotNull(season);

            // Act
            var result = await _controller.SeasonDetails(season.Id) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<Season>(result.Model);
            Assert.Equal(season.Id, model.Id);
            Assert.NotNull(model.Episodes);
            Assert.Equal(season.Episodes.Count, model.Episodes.Count);
        }

        [Fact]
        public async Task SeasonDetails_ReturnsNotFound_WhenSeasonDoesNotExist()
        {
            // Arrange: Use a non-existent season id
            int nonExistentId = 9999;

            // Act
            var result = await _controller.SeasonDetails(nonExistentId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        [Fact]
        public async Task EpisodeDetails_ReturnsViewResult_WithEpisode()
        {
            // Arrange: Get existing episode id
            var episode = await _context.Episodes.Include(e => e.Season).ThenInclude(s => s.Series).FirstOrDefaultAsync();
            Assert.NotNull(episode);

            // Act
            var result = await _controller.EpisodeDetails(episode.Id) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<Episode>(result.Model);
            Assert.Equal(episode.Id, model.Id);
            Assert.NotNull(model.Season);
            Assert.NotNull(model.Season.Series);
        }

        [Fact]
        public async Task EpisodeDetails_ReturnsNotFound_WhenEpisodeDoesNotExist()
        {
            // Arrange: Use a non-existent episode id
            int nonExistentId = 9999;

            // Act
            var result = await _controller.EpisodeDetails(nonExistentId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
