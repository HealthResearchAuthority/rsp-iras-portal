using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Xunit.Sdk;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications;

public class DeleteModificationTests : TestServiceBase<ModificationsController>
{
    private readonly Mock<IProjectModificationsService> _modsService;

    public DeleteModificationTests()
    {
        _modsService = Mocker.GetMock<IProjectModificationsService>();
    }

    [Theory]
    [AutoData]
    public async Task DeleteModification_Returns_View_With_Details_When_Service_Succeeds(
        string projectRecordId,
        string irasId,
        string shortTitle)
    {
        // Arrange
        var projectModificationId = Guid.NewGuid();

        var serviceContent = new GetModificationsResponse
        {
            Modifications =
            [
                new ModificationsDto
                {
                    Id = projectModificationId.ToString(),
                    ModificationId = "90000/1",
                    Status = ModificationStatus.ModificationRecordStarted
                }
            ]
        };

        _modsService
            .Setup(s => s.GetModificationsByIds(
                It.Is<List<string>>(ids => ids.Count == 1 && ids[0] == projectModificationId.ToString())))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = serviceContent
            });

        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.DeleteModification(projectRecordId, irasId, shortTitle, projectModificationId);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBeNull(); // default view
        var model = view.Model.ShouldBeOfType<ModificationDetailsViewModel>();

        model.ModificationId.ShouldBe(projectModificationId.ToString());
        model.IrasId.ShouldBe(irasId);
        model.ShortTitle.ShouldBe(shortTitle);
        model.ModificationIdentifier.ShouldBe("90000/1");
        model.Status.ShouldBe(ModificationStatus.ModificationRecordStarted);
        model.ProjectRecordId.ShouldBe(projectRecordId);

        // TempData set for subsequent steps
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier].ShouldBe("90000/1");
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationId].ShouldBe(projectModificationId.ToString());

        // Verify call
        _modsService.Verify(s =>
                s.GetModificationsByIds(It.Is<List<string>>(ids => ids.Single() == projectModificationId.ToString())),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task DeleteModification_Populates_ProblemDetails_And_Returns_Status_When_Service_Fails(
        string projectRecordId,
        string irasId,
        string shortTitle)
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };

        var projectModificationId = Guid.NewGuid();

        _modsService
            .Setup(s => s.GetModificationsByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.BadGateway,
                Error = "Upstream failure"
            });

        // Act
        var result = await Sut.DeleteModification(projectRecordId, irasId, shortTitle, projectModificationId);

        // Assert status
        AssertStatusCode(result, StatusCodes.Status502BadGateway);


        _modsService.Verify(s =>
                s.GetModificationsByIds(It.Is<List<string>>(ids => ids.Single() == projectModificationId.ToString())),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task DeleteModification_Populates_ProblemDetails_And_Returns_400_When_No_Modification_Found(
        string irasId,
        string shortTitle)
    {
        // Arrange
        var projectRecordId = "PR-123";
        var projectModificationId = Guid.NewGuid();

        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };

        _modsService
            .Setup(s => s.GetModificationsByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new GetModificationsResponse { Modifications = [] }
            });

        // Act
        var result = await Sut.DeleteModification(projectRecordId, irasId, shortTitle, projectModificationId);

        // Assert status
        AssertStatusCode(result, StatusCodes.Status400BadRequest);

        _modsService.Verify(s =>
                s.GetModificationsByIds(It.Is<List<string>>(ids => ids.Single() == projectModificationId.ToString())),
            Times.Once);
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