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
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("HomeTestDb")
                .Options;
            var context = new AppDbContext(options);

            if (!context.Movies.Any())
            {
                context.Movies.Add(new Movie { Id = 1, Title = "Movie1", Rating = 5 });
                context.Movies.Add(new Movie { Id = 2, Title = "Movie2", Rating = 8 });
                context.SaveChanges();
            }

            return context;
        }

        [Fact]
        public async Task Index_ReturnsTopMovies()
        {
            var context = GetDbContext();
            var logger = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(logger.Object, context);

            var result = await controller.Index() as ViewResult;
            var model = result.Model as List<Movie>;

            Assert.NotNull(model);
            Assert.Equal(2, model.Count);
            Assert.Equal(8, model.First().Rating); // highest rating first
        }

        [Fact]
        public void Privacy_ReturnsView()
        {
            var context = GetDbContext();
            var logger = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(logger.Object, context);

            var result = controller.Privacy() as ViewResult;

            Assert.NotNull(result);
        }
    }
}
