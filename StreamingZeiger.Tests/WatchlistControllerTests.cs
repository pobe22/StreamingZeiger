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
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "user1")
                        }, "mock"))
                    }
                }
            };

            var result = await controller.Add(1) as RedirectResult;

            Assert.NotNull(result);
            Assert.Equal("/", result.Url); 
            Assert.Contains(context.WatchlistItems, w => w.MediaItemId == 1 && w.UserId == "user1");
        }

        [Fact]
        public async Task Add_ReturnsNotFound_WhenMediaItemDoesNotExist()
        {
            var context = GetDbContext("NoMediaItemDb");
            var userManager = GetUserManagerMock();
            var user = new ApplicationUser { Id = "user1" };
            userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var controller = new WatchlistController(context, userManager.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "user1")
                        }, "mock"))
                    }
                }
            };

            var result = await controller.Add(99);
            Assert.IsType<NotFoundObjectResult>(result);
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
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "user1")
                        }, "mock"))
                    }
                }
            };

            var result = await controller.Remove(1) as RedirectResult;

            Assert.NotNull(result);
            Assert.Equal("/", result.Url);
            Assert.DoesNotContain(context.WatchlistItems, w => w.MediaItemId == 1 && w.UserId == "user1");
        }

        [Fact]
        public async Task Remove_DoesNothing_WhenItemNotExists()
        {
            var context = GetDbContext("RemoveDoesNothingDb");

            var userManager = GetUserManagerMock();
            var user = new ApplicationUser { Id = "user1" };
            userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var controller = new WatchlistController(context, userManager.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "user1")
                        }, "mock"))
                    }
                }
            };

            var result = await controller.Remove(42) as RedirectResult;

            Assert.NotNull(result);
            Assert.Equal("/", result.Url);
            Assert.Empty(context.WatchlistItems); 
        }
    }
}
