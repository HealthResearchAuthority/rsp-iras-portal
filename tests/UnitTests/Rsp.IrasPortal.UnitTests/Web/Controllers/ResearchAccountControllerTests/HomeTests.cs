using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ResearchAccountControllerTests;

public class HomeTests : TestServiceBase<ResearchAccountController>
{
    [Fact]
    public void Home_Should_Return_Index_View_When_LastLogin_Is_Null()
    {
        // Arrange
        var session = new Mock<ISession>();

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = Sut.Home();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Index));
    }

    [Fact]
    public void Home_Should_Format_LastLogin_And_Return_View_With_Model()
    {
        // Arrange

        var session = new Mock<ISession>();

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var lastLoginUtc = new DateTime(2024, 4, 4, 10, 42, 0, DateTimeKind.Utc);
        httpContext.Items[ContextItemKeys.LastLogin] = lastLoginUtc;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var ukDateTime = TimeZoneInfo.ConvertTimeFromUtc(lastLoginUtc, ukTimeZone);

        var expectedDate = ukDateTime.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var expectedTime = ukDateTime.ToString("h:mmtt", CultureInfo.InvariantCulture).ToLowerInvariant();
        var expectedModel = $"{expectedDate} at {expectedTime} UK time";

        // Act
        var result = Sut.Home();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");
        viewResult.Model.ShouldBe(expectedModel);
    }

    [Fact]
    public void Home_Should_Return_Index_When_User_Status_Claim_Is_Null()
    {
        // Arrange
        var session = new Mock<ISession>();

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object,
            User = CreateAuthenticatedPrincipalWithoutUserStatusClaim()
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = Sut.Home();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Index));
    }

    [Fact]
    public void Home_Should_Return_Forbidden_When_User_Status_Is_Disabled()
    {
        // Arrange
        var session = new Mock<ISession>();

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object,
            User = CreateAuthenticatedPrincipalWithDisabledStatus()
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = Sut.Home();

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public void Home_Should_Return_Index_View_When_User_Is_Authenticated_And_Active()
    {
        // Arrange
        var session = new Mock<ISession>();

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object,
            User = CreateAuthenticatedPrincipalWithActiveStatus()
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = Sut.Home();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Index));
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipalWithoutUserStatusClaim()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "Test User")
        };

        var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType", ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(claimsIdentity);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipalWithDisabledStatus()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "Test User"),
            new(CustomClaimTypes.UserStatus, IrasUserStatus.Disabled)
        };

        var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType", ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(claimsIdentity);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipalWithActiveStatus()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "Test User"),
            new(CustomClaimTypes.UserStatus, IrasUserStatus.Active)
        };

        var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType", ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(claimsIdentity);
    }
}