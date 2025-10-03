using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ReviewAllChangesControllerTests;

public class SendModificationToSponsor : TestServiceBase<ReviewAllChangesController>
{
    [Theory, AutoData]
    public async Task SendModificationToSponsor_Should_Return_View_When_Success(
        string projectRecordId,
        Guid projectModificationId)
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var response = new ServiceResponse
        {
            StatusCode = HttpStatusCode.OK
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(projectModificationId, ModificationStatus.InSponsorReview))
            .ReturnsAsync(response);

        // Act
        var result = await Sut.SendModificationToSponsor(projectRecordId, projectModificationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ModificationSentToSponsor");

        // Verify
        Mocker.GetMock<IProjectModificationsService>()
            .Verify(s => s.UpdateModificationStatus(projectModificationId, ModificationStatus.InSponsorReview), Times.Once);
    }

    [Theory, AutoData]
    public async Task SubmitToRegulator_Should_Redirect_When_Success(
        string projectRecordId,
        Guid projectModificationId)
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var response = new ServiceResponse
        {
            StatusCode = HttpStatusCode.OK
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(projectModificationId, ModificationStatus.Approved))
            .ReturnsAsync(response);

        // Act
        var result = await Sut.SubmitToRegulator(projectRecordId, projectModificationId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pov:projectdetails");
        redirectResult.RouteValues.ShouldContainKeyAndValue("projectRecordId", projectRecordId);

        // Verify
        Mocker.GetMock<IProjectModificationsService>()
            .Verify(s => s.UpdateModificationStatus(projectModificationId, ModificationStatus.Approved), Times.Once);
    }

    [Theory, AutoData]
    public async Task SendModificationToSponsor_Should_Return_StatusCode_When_Failure(
        string projectRecordId,
        Guid projectModificationId)
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var response = new ServiceResponse
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(projectModificationId, ModificationStatus.InSponsorReview))
            .ReturnsAsync(response);

        // Act
        var result = await Sut.SendModificationToSponsor(projectRecordId, projectModificationId);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Theory, AutoData]
    public async Task SubmitToRegulator_Should_Return_StatusCode_When_Failure(
        string projectRecordId,
        Guid projectModificationId)
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var response = new ServiceResponse
        {
            StatusCode = HttpStatusCode.BadRequest
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(projectModificationId, ModificationStatus.Approved))
            .ReturnsAsync(response);

        // Act
        var result = await Sut.SubmitToRegulator(projectRecordId, projectModificationId);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }
}