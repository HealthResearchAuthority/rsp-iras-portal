using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
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
                new Claim(ClaimTypes.Name, "testUser")
            ], "mock")
        );

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task MyApplications_WhenFeatureEnabled_AndServiceReturnsSuccess_ReturnsViewWithApplications()
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<IrasApplicationResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new List<IrasApplicationResponse>
            {
                new IrasApplicationResponse { Id = "1234567890" }
            },
            new RefitSettings()
        );

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplicationsByRespondent(It.IsAny<string>()))
            .ReturnsAsync(apiResponse.ToServiceResponse());

        var apiResponseQuestions = new ApiResponse<IEnumerable<CategoryDto>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new List<CategoryDto>
            {
                new()
                {
                    CategoryId = "A",
                    CategoryName = "Test"
                }
            },
            new RefitSettings()
        );

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionCategories()).ReturnsAsync(apiResponseQuestions.ToServiceResponse());

        // Act
        var result = await Sut.MyApplications();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldNotBeNull();
    }

    [Fact]
    public async Task MyApplications_WhenServiceReturnsError_ReturnsServiceErrorResult()
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<IrasApplicationResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            null,
            new RefitSettings()
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
        var apiResponse = new ApiResponse<IEnumerable<IrasApplicationResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            [],
            new RefitSettings()
        );

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplicationsByRespondent(It.IsAny<string>()))
            .ReturnsAsync(apiResponse.ToServiceResponse());

        var apiResponseQuestions = new ApiResponse<IEnumerable<CategoryDto>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new List<CategoryDto>
            {
                new()
                {
                    CategoryId = "A",
                    CategoryName = "Test"
                }
            },
            new RefitSettings()
        );

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionCategories()).ReturnsAsync(apiResponseQuestions.ToServiceResponse());

        var session = new Mock<ISession>();
        Sut.ControllerContext.HttpContext.Session = session.Object;

        // Act
        await Sut.MyApplications();

        // Assert
        session.Verify(s => s.Remove(SessionKeys.ProjectRecord), Times.Once);
    }
}