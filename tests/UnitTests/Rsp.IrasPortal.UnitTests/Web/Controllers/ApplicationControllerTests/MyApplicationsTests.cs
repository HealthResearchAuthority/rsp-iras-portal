using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class MyApplicationsTests : TestServiceBase<ApplicationController>
{
    public MyApplicationsTests()
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
    public async Task MyApplications_WhenFeatureEnabled_AndServiceReturnsSuccess_ReturnsViewWithApplications(IEnumerable<IrasApplicationResponse> applications)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<IrasApplicationResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            applications,
            new()
        );

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplicationsByRespondent(It.IsAny<string>()))
            .ReturnsAsync(apiResponse.ToServiceResponse());

        // Act
        var result = await Sut.MyApplications();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBe(applications);
    }

    [Fact]
    public async Task MyApplications_WhenServiceReturnsError_ReturnsServiceErrorResult()
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<IrasApplicationResponse>>
        (
           new HttpResponseMessage(HttpStatusCode.InternalServerError),
           null,
           new()
        );

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplicationsByRespondent(It.IsAny<string>()))
            .ReturnsAsync(apiResponse.ToServiceResponse());

        // Act
        var result = await Sut.MyApplications();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
    }

    [Fact]
    public async Task MyApplications_ClearsSessionValues()
    {
        // Arrange
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<IrasApplicationResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            [],
            new()
        );

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplicationsByRespondent(It.IsAny<string>()))
            .ReturnsAsync(apiResponse.ToServiceResponse());

        var session = new Mock<ISession>();
        Sut.ControllerContext.HttpContext.Session = session.Object;

        // Act
        await Sut.MyApplications();

        // Assert
        session.Verify(s => s.Remove(SessionKeys.Application), Times.Once);
    }
}