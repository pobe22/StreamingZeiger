using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using StreamingZeiger.Controllers;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace StreamingZeiger.Tests
{
    public class AdminControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            var context = new AppDbContext(options);

            if (!context.Movies.Any())
            {
                context.Movies.Add(new Movie { Id = 1, Title = "Test Movie" });
                context.SaveChanges();
            }

            return context;
        }

        private Mock<IWebHostEnvironment> GetMockEnv()
        {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.WebRootPath).Returns(Directory.GetCurrentDirectory());
            return env;
        }

        [Fact]
        public async Task Index_ReturnsViewWithMovies()
        {
            var context = GetDbContext();
            var env = GetMockEnv();
            var controller = new AdminController(context, env.Object);

            var result = await controller.Index() as ViewResult;
            var model = result.Model as List<Movie>;

            Assert.NotNull(result);
            Assert.NotEmpty(model);
        }

        [Fact]
        public void Create_Get_ReturnsView()
        {
            var context = GetDbContext();
            var env = GetMockEnv();
            var controller = new AdminController(context, env.Object);

            var result = controller.Create() as ViewResult;

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Create_Post_ValidMovie_AddsMovie()
        {
            var context = GetDbContext();
            var env = GetMockEnv();
            var controller = new AdminController(context, env.Object);

            // TempData initialisieren
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>()
            );
            controller.TempData = tempData;

            var movie = new Movie { Title = "New Movie" };
            var services = new List<string> { "Netflix" };

            var result = await controller.Create(movie, "Actor1, Actor2", services, "Action, Drama", null) as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);
            Assert.Contains(context.Movies, m => m.Title == "New Movie");
            Assert.Equal("Film erfolgreich hinzugefügt.", controller.TempData["Message"]);
        }

        [Fact]
        public async Task Edit_Get_ReturnsView_WhenMovieExists()
        {
            var context = GetDbContext();
            var env = GetMockEnv();
            var controller = new AdminController(context, env.Object);

            var result = await controller.Edit(1) as ViewResult;

            Assert.NotNull(result);
            var movie = result.Model as Movie;
            Assert.Equal(1, movie.Id);
        }

        [Fact]
        public async Task DeleteConfirmed_RemovesMovie()
        {
            var context = GetDbContext();
            var env = GetMockEnv();
            var controller = new AdminController(context, env.Object);

            var result = await controller.DeleteConfirmed(1) as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);
            Assert.DoesNotContain(context.Movies, m => m.Id == 1);
        }

        [Fact]
        public async Task ImportFromTmdb_ReturnsJsonResult()
        {
            var context = GetDbContext();
            var env = GetMockEnv();
            var controller = new AdminController(context, env.Object);

            var result = await controller.ImportFromTmdb(550) as JsonResult;

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            var context = GetDbContext();
            var env = GetMockEnv();
            var controller = new AdminController(context, env.Object);
            controller.ModelState.AddModelError("Title", "Required");

            var movie = new Movie();
            var result = await controller.Create(movie, "", new List<string>(), "", null) as ViewResult;

            Assert.NotNull(result);
            Assert.Equal(movie, result.Model);
        }
    }
}
