using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.AuthControllerTests
{
    public class SignoutTests : TestServiceBase<AuthController>
    {
        [Fact]
        public async Task Signout_Should_SignOut_From_Both_Schemes_And_Return_SignOutResult()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>();

            var authenticationService = new Mock<IAuthenticationService>();
            authenticationService
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            serviceProvider
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authenticationService.Object);

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider.Object
            };

            httpContext.Request.Path = "/Auth/Signout";

            var urlHelper = new Mock<IUrlHelper>();

            Sut.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            Sut.Url = urlHelper.Object;

            urlHelper
                .Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Returns("/welcome");

            // Act
            var result = await Sut.Signout();

            // Assert
            var signOutResult = result.ShouldBeOfType<SignOutResult>();
            signOutResult.AuthenticationSchemes.ShouldContain(CookieAuthenticationDefaults.AuthenticationScheme);
            signOutResult.AuthenticationSchemes.ShouldContain("OpenIdConnect");
            signOutResult
                .Properties.ShouldNotBeNull()
                .RedirectUri.ShouldBe("/welcome");

            // Verify
            urlHelper.Verify(u => u.RouteUrl(It.Is<UrlRouteContext>(c => c.RouteName == "app:welcome")), Times.Once);
        }
    }
}