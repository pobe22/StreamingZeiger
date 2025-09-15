using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StreamingZeiger.Controllers;
using StreamingZeiger.Models;
using StreamingZeiger.ViewModels;
using System.Threading.Tasks;
using Xunit;

namespace StreamingZeiger.Tests
{
    public class AccountControllerTests
    {
        private Mock<UserManager<ApplicationUser>> GetUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<SignInManager<ApplicationUser>> GetSignInManagerMock(Mock<UserManager<ApplicationUser>> um)
        {
            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            return new Mock<SignInManager<ApplicationUser>>(um.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }

        [Fact]
        public async Task Register_Post_Valid_Redirects()
        {
            var um = GetUserManagerMock();
            var sm = GetSignInManagerMock(um);
            um.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
              .ReturnsAsync(IdentityResult.Success);
            sm.Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), false, null))
              .Returns(Task.CompletedTask);

            var controller = new AccountController(um.Object, sm.Object);

            var result = await controller.Register(new RegisterViewModel { Email = "a@b.com", Password = "Password123!" })
                as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Movies", result.ControllerName);
        }

        [Fact]
        public void Register_Get_ReturnsView()
        {
            var um = GetUserManagerMock();
            var sm = GetSignInManagerMock(um);
            var controller = new AccountController(um.Object, sm.Object);

            var result = controller.Register() as ViewResult;

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Login_Post_InvalidModel_ReturnsView()
        {
            var um = GetUserManagerMock();
            var sm = GetSignInManagerMock(um);
            var controller = new AccountController(um.Object, sm.Object);
            controller.ModelState.AddModelError("Email", "Required");

            var result = await controller.Login(new LoginViewModel()) as ViewResult;

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Logout_RedirectsToMoviesIndex()
        {
            var um = GetUserManagerMock();
            var sm = GetSignInManagerMock(um);
            sm.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

            var controller = new AccountController(um.Object, sm.Object);
            var result = await controller.Logout() as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Movies", result.ControllerName);
        }
    }
}
