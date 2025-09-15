using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StreamingZeiger.Controllers;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace StreamingZeiger.Tests
{
    public class RatingControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("RatingTestDb")
                .Options;
            var context = new AppDbContext(options);
            context.Movies.Add(new Movie { Id = 1, Title = "Movie1" });
            context.SaveChanges();
            return context;
        }

        private Mock<UserManager<ApplicationUser>> GetUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task Add_CreatesRating()
        {
            var context = GetDbContext();
            var userManager = GetUserManagerMock();
            var user = new ApplicationUser { Id = "user1" };
            userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var controller = new RatingController(context, userManager.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                    {
                        User = new ClaimsPrincipal()
                    }
                }
            };

            var result = await controller.Add(1, 5) as RedirectToActionResult;

            Assert.Equal("Details", result.ActionName);
            Assert.Equal("Movies", result.ControllerName);
            Assert.Contains(context.Ratings, r => r.MovieId == 1 && r.UserId == "user1" && r.Score == 5);
        }
    }
}
