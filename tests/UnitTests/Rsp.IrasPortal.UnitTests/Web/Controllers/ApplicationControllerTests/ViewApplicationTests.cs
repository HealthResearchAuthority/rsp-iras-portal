using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class ViewApplicationTests : TestServiceBase<ApplicationController>
{
    public ViewApplicationTests()
    {
        // Setup HttpContext and User
        var httpContext = new DefaultHttpContext
        {
            Session = Mock.Of<ISession>()
        };

        httpContext.Items[ContextItemKeys.RespondentId] = "testRespondentId";
        httpContext.User = new ClaimsPrincipal
        (
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "testUser"),
            ], "mock")
        );

        Sut.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };
    }

    [Theory, AutoData]
    public async Task ViewApplication_WhenModelStateIsValid_AndServiceReturnsSuccess_ReturnsViewWithApplication
    (
        string applicationId,
        IrasApplicationResponse application
    )
    {
        // Arrange
        var apiResponse = new ApiResponse<IrasApplicationResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            application,
            new()
        );

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(apiResponse.ToServiceResponse());

        // Act
        var result = await Sut.ViewApplication(applicationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ApplicationView");
        viewResult.Model.ShouldBe(application);
    }

    [Fact]
    public async Task ViewApplication_WhenModelStateIsInvalid_ReturnsViewWithNullModel()
    {
        // Arrange
        Sut.ModelState.AddModelError("Error", "Sample error");

        // Act
        var result = await Sut.ViewApplication("anyId");

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ApplicationView");
        viewResult.Model.ShouldBeNull();
    }

    [Theory, AutoData]
    public async Task ViewApplication_WhenServiceReturnsError_ReturnsServiceErrorResult(string applicationId)
    {
        // Arrange
        var apiResponse = new ApiResponse<IrasApplicationResponse>
        (
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            null,
            new()
        );

        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(apiResponse.ToServiceResponse());

        // Act
        var result = await Sut.ViewApplication(applicationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
    }
}