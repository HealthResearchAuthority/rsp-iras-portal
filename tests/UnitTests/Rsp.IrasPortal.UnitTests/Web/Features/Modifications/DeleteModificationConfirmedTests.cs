using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;
using Xunit.Sdk;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications;

public class DeleteModificationConfirmedTests : TestServiceBase<ModificationsController>
{
    private readonly Mock<IProjectModificationsService> _modsService;

    public DeleteModificationConfirmedTests()
    {
        _modsService = Mocker.GetMock<IProjectModificationsService>();
    }

    [Theory]
    [AutoData]
    public async Task DeleteModificationConfirmed_Should_Redirect_With_Banner_When_Service_Succeeds(
        string projectRecordId)
    {
        // Arrange
        var projectModificationId = Guid.NewGuid();
        var projectModificationIdentifier = "90000/1";

        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        _modsService
            .Setup(s => s.DeleteModification(projectModificationId))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // Act
        var result =
            await Sut.DeleteModificationConfirmed(projectRecordId, projectModificationId,
                projectModificationIdentifier);

        // Assert: redirect and route values
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:index");
        redirect.RouteValues.ShouldNotBeNull();
        redirect.RouteValues!["projectRecordId"].ShouldBe(projectRecordId);
        redirect.RouteValues!["modificationId"].ShouldBe(projectModificationIdentifier); // uses identifier, not Guid

        // Assert: TempData flags set
        Sut.TempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldBe(projectModificationId);

        // No ProblemDetails for success path
        http.Items.ContainsKey(ContextItemKeys.ProblemDetails).ShouldBeFalse();

        // Verify service interaction
        _modsService.Verify(s => s.DeleteModification(projectModificationId), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task
        DeleteModificationConfirmed_Should_Return_ServiceError_And_Populate_ProblemDetails_When_Service_Fails(
            string projectRecordId)
    {
        // Arrange
        var projectModificationId = Guid.NewGuid();
        var projectModificationIdentifier = "ANY-ID";

        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        _modsService
            .Setup(s => s.DeleteModification(projectModificationId))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadGateway,
                Error = "Upstream failure"
            });

        // Act
        var result =
            await Sut.DeleteModificationConfirmed(projectRecordId, projectModificationId,
                projectModificationIdentifier);

        // Assert: correct status
        AssertStatusCode(result, StatusCodes.Status502BadGateway);

        // Assert: TempData NOT set on failure
        Sut.TempData.ContainsKey(TempDataKeys.ShowNotificationBanner).ShouldBeFalse();
        Sut.TempData.ContainsKey(TempDataKeys.ProjectModification.ProjectModificationChangeMarker).ShouldBeFalse();

        // Verify service interaction
        _modsService.Verify(s => s.DeleteModification(projectModificationId), Times.Once);
    }

    /// <summary>
    ///     Helper to assert status code regardless of whether ServiceError returns StatusCodeResult or ObjectResult.
    /// </summary>
    private static void AssertStatusCode(IActionResult result, int expected)
    {
        if (result is StatusCodeResult sc)
        {
            sc.StatusCode.ShouldBe(expected);
            return;
        }

        if (result is ObjectResult oc && oc.StatusCode.HasValue)
        {
            oc.StatusCode.Value.ShouldBe(expected);
            return;
        }

        throw new XunitException($"Unexpected result type: {result.GetType().Name}");
    }
}