using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationsReviewControllerTests;

public class GetApplicationTests : TestServiceBase<ApplicationsReviewController>
{
    [Fact]
    public async Task GetApplication_Should_Return_ApplicationReview_View_With_Null_Model_When_ModelState_Is_Invalid()
    {
        // Arrange
        Sut.ModelState.AddModelError("applicationId", "Invalid application ID");

        // Act
        var result = await Sut.GetApplication("invalidId");

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ApplicationReview");
        viewResult.Model.ShouldBeNull();

        // Verify
        Mocker
            .GetMock<IApplicationsService>()
            .Verify(s => s.GetApplicationByStatus(It.IsAny<string>(), It.IsAny<string>()), Times.Never()
        );
    }

    [Theory, AutoData]
    public async Task GetApplication_Should_Return_ApplicationReview_View_With_Correct_Model_When_Application_Is_Found(string applicationId, IrasApplicationResponse irasApplicationResponse)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = irasApplicationResponse
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplicationByStatus(It.IsAny<string>(), "pending"))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.GetApplication(applicationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ApplicationReview");
        viewResult.Model.ShouldBe(irasApplicationResponse);

        // Verify
        Mocker
            .GetMock<IApplicationsService>()
            .Verify(s => s.GetApplicationByStatus(applicationId, "pending"), Times.Once());
    }

    [Theory, AutoData]
    public async Task GetApplication_Should_Return_ServiceError_When_ApplicationsService_Returns_NonSuccess_Status(string applicationId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/ApplicationsReview/GetApplication";

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var failureResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.InternalServerError,
            ReasonPhrase = "Internal Server Error",
            Content = null,
            Error = "An error occurred while processing your request"
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplicationByStatus(applicationId, "pending"))
            .ReturnsAsync(failureResponse);

        // Act
        var result = await Sut.GetApplication(applicationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");

        // Verify
        Mocker
            .GetMock<IApplicationsService>()
            .Verify(s => s.GetApplicationByStatus(applicationId, "pending"), Times.Once());
    }
}