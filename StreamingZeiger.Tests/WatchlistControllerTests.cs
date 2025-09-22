using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StreamingZeiger.Controllers;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace StreamingZeiger.Tests
{
    public class WatchlistControllerTests
    {
        private AppDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            var context = new AppDbContext(options);

            context.Movies.Add(new Movie
            {
                Id = 1,
                Title = "Movie1",
                Year = 2023,
                DurationMinutes = 120
            });

            context.SaveChanges();
            return context;
        }

        private Mock<UserManager<ApplicationUser>> GetUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task Add_AddsItem()
        {
            var context = GetDbContext("AddAddsItemDb");
            var userManager = GetUserManagerMock();
            var user = new ApplicationUser { Id = "user1" };
            userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var controller = new WatchlistController(context, userManager.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                    {
                        User = new ClaimsPrincipal()
                    }
                }
            };

            var result = await controller.Add(1) as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);
            Assert.Contains(context.WatchlistItems, w => w.MediaItemId == 1 && w.UserId == "user1");
        }


        [Fact]
        public async Task Remove_RemovesItem()
        {
            var context = GetDbContext("RemoveRemovesItemDb");
            context.WatchlistItems.Add(new WatchlistItem { MediaItemId = 1, UserId = "user1" });
            context.SaveChanges();

            var userManager = GetUserManagerMock();
            var user = new ApplicationUser { Id = "user1" };
            userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var controller = new WatchlistController(context, userManager.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                    {
                        User = new ClaimsPrincipal()
                    }
                }
            };

            var result = await controller.Remove(1) as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);
            Assert.DoesNotContain(context.WatchlistItems, w => w.MediaItemId == 1 && w.UserId == "user1");
        }
    }
}
