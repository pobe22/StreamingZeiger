using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using System.Security.Claims;

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
            return new Mock<UserManager<ApplicationUser>>(store.Object, new Mock<IOptions<IdentityOptions>>().Object,
               new Mock<IPasswordHasher<ApplicationUser>>().Object,
               new IUserValidator<ApplicationUser>[0],
               new IPasswordValidator<ApplicationUser>[0],
               new Mock<ILookupNormalizer>().Object,
               new Mock<IdentityErrorDescriber>().Object,
               new Mock<IServiceProvider>().Object,
               new Mock<ILogger<UserManager<ApplicationUser>>>().Object);
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
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                        new Claim[] { new Claim(ClaimTypes.NameIdentifier, user.Id) },
                        authenticationType: "TestAuthType"
                        ))
                    }
                }
            };

            var result = await controller.Add(1, 5) as RedirectToActionResult;
            if (result == null)
            {
                throw new Xunit.Sdk.XunitException("Expected RedirectToActionResult, but got null.");
            }

            Assert.Equal("Details", result.ActionName);
            Assert.Equal("Movies", result.ControllerName);
            Assert.Contains(context.Ratings, r => r.MediaItemId == 1 && r.UserId == "user1" && r.Score == 5);
        }
    }
}
