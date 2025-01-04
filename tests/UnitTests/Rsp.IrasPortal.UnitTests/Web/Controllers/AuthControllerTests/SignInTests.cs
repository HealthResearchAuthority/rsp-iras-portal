using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.AuthControllerTests;

public class SignInTests : TestServiceBase<AuthController>
{
    [Fact]
    public void SignIn_Should_Return_ChallengeResult_With_OpenIdConnect_Scheme()
    {
        // Arrange
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Returns("http://example.com/welcome");

        Sut.Url = urlHelper.Object;

        // Act
        var result = Sut.SignIn();

        // Assert
        var challengeResult = result.ShouldBeOfType<ChallengeResult>();
        challengeResult.AuthenticationSchemes.ShouldContain("OpenIdConnect");
        challengeResult
            .Properties.ShouldNotBeNull()
            .RedirectUri.ShouldBe("http://example.com/welcome");

        // Verify
        urlHelper.Verify(u => u.RouteUrl(It.IsAny<UrlRouteContext>()), Times.Once());
    }
}