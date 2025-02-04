using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationsReviewControllerTests;

public class PendingApplicationsTests : TestServiceBase<ApplicationsReviewController>
{
    [Theory, AutoData]
    public async Task PendingApplications_Should_Return_View_With_Pending_Applications_When_Service_Call_Is_Successful
    (
        List<IrasApplicationResponse> applications
    )
    {
        // Arrange
        var successResponse = new ServiceResponse<IEnumerable<IrasApplicationResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = applications
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplicationsByStatus("pending"))
            .ReturnsAsync(successResponse);

        // Act
        var result = await Sut.PendingApplications();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBe(successResponse.Content);

        // Verify
        Mocker
            .GetMock<IApplicationsService>()
            .Verify(s => s.GetApplicationsByStatus("pending"), Times.Once());
    }

    [Fact]
    public async Task PendingApplications_Should_Return_Generic_Error_Page_When_Service_Call_Fails()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/ApplicationsReview/PendingApplications";

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var failureResponse = new ServiceResponse<IEnumerable<IrasApplicationResponse>>
        {
            StatusCode = HttpStatusCode.InternalServerError,
            ReasonPhrase = "Internal Server Error",
            Content = null,
            Error = "An error occurred while processing your request"
        };

        Mocker
        .GetMock<IApplicationsService>()
        .Setup(s => s.GetApplicationsByStatus("pending"))
        .ReturnsAsync(failureResponse);

        // Act
        var result = await Sut.PendingApplications();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");

        // Verify
        Mocker
        .GetMock<IApplicationsService>()
        .Verify(s => s.GetApplicationsByStatus("pending"), Times.Once());
    }
}