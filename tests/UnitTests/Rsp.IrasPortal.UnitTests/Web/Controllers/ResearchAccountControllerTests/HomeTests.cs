using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ResearchAccountControllerTests;

public class HomeTests : TestServiceBase<ResearchAccountController>
{
    [Fact]
    public void Home_Should_Return_Index_View_When_LastLogin_Is_Null()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

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
        var httpContext = new DefaultHttpContext();

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
}