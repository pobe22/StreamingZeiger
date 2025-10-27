using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StreamingZeiger.Controllers;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace StreamingZeiger.Tests
{
    public class HomeControllerTests
    {
        private AppDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            var context = new AppDbContext(options);

            // Movies initialisieren
            if (!context.Movies.Any())
            {
                context.Movies.Add(new Movie { Id = 1, Title = "Movie1", Rating = 5, Year = 2020 });
                context.Movies.Add(new Movie { Id = 2, Title = "Movie2", Rating = 8, Year = 2021 });
                context.SaveChanges();
            }

            // Series initialisieren
            if (!context.Series.Any())
            {
                var series = new Series
                {
                    Id = 3,
                    Title = "Breaking Bad",
                    StartYear = 2008,
                    EndYear = 2013,
                    Seasons = new List<Season>
            {
                new Season
                {
                    SeasonNumber = 1,
                    Episodes = new List<Episode>
                    {
                        new Episode { EpisodeNumber = 1, Title = "Pilot", DurationMinutes = 58 }
                    }
                }
            }
                };
                context.Series.Add(series);
                context.SaveChanges();
            }

            return context;
        }

        [Fact]
        public async Task Index_ReturnsTopMoviesAndSeries()
        {
            // Arrange
            var context = GetDbContext("HomeTestDb_Index");
            var logger = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(logger.Object, context);

            // Act
            var result = await controller.Index() as ViewResult;
            var model = result.Model as StreamingZeiger.ViewModels.AdminIndexViewModel;

            // Assert
            Assert.NotNull(model);

            // Prüfe Filme
            Assert.NotNull(model.Movies);
            Assert.Equal(2, model.Movies.Count);
            Assert.Equal(8, model.Movies.First().Rating);

            // Prüfe Serien
            Assert.NotNull(model.Series);
            Assert.Single(model.Series);
            Assert.Equal("Breaking Bad", model.Series.First().Title);
        }
    }
}
