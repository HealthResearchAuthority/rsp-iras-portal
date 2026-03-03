using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.UnitTests;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Features.Modifications.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ReviewAllChangesControllerTests;

public class DuplicateModificationTests : TestServiceBase<ReviewAllChangesController>
{
    private readonly Mock<IProjectModificationsService> _modificationService;

    public DuplicateModificationTests()
    {
        _modificationService = Mocker.GetMock<IProjectModificationsService>();
    }

    [Fact]
    public async Task DuplicateModification_Get_ReturnsError_When_NoTempData()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.DuplicateModification();

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(404);
    }

    [Theory]
    [AutoData]
    public async Task DuplicateModification_Get_RedirectsToReviewAllChanges_When_GetAndDuplicateSucceed
    (
        ReviewOutcomeViewModel model,
        string projectRecordId,
        string irasId,
        string shortTitle,
        Guid existingModificationId,
        Guid duplicatedModificationId
    )
    {
        // Arrange
        model.ModificationDetails.ProjectRecordId = projectRecordId;
        model.ModificationDetails.IrasId = irasId;
        model.ModificationDetails.ShortTitle = shortTitle;
        model.ModificationDetails.ModificationId = existingModificationId.ToString();
        SetupTempData(model);

        var currentModification = new ProjectModificationResponse
        {
            Id = existingModificationId,
            ProjectRecordId = projectRecordId
        };

        _modificationService
            .Setup(s => s.GetModification(projectRecordId, existingModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = currentModification
            });

        _modificationService
            .Setup(s => s.DuplicateModification(It.IsAny<DuplicateModificationRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationResponse { Id = duplicatedModificationId }
            });

        // Act
        var result = await Sut.DuplicateModification();

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(ReviewAllChangesController.ReviewAllChanges));

        redirect.RouteValues.ShouldNotBeNull();
        redirect.RouteValues["projectRecordId"].ShouldBe(projectRecordId);
        redirect.RouteValues["irasId"].ShouldBe(irasId);
        redirect.RouteValues["shortTitle"].ShouldBe(shortTitle);
        redirect.RouteValues["projectModificationId"].ShouldBe(duplicatedModificationId);

        // TempData flags
        Sut.TempData[TempDataKeys.IsDuplicateModification].ShouldBe(true);
        Sut.TempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);

        // Service calls & request correctness
        _modificationService.Verify(s => s.GetModification(projectRecordId, existingModificationId), Times.Once);

        _modificationService.Verify(s => s.DuplicateModification(It.Is<DuplicateModificationRequest>(r =>
            r.ProjectRecordId == projectRecordId &&
            r.ExistingModificationId == existingModificationId
        )), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task DuplicateModification_Get_ReturnsError_When_GetModification_Fails
    (
        ReviewOutcomeViewModel model,
        string projectRecordId,
        Guid existingModificationId
    )
    {
        // Arrange
        model.ModificationDetails.ProjectRecordId = projectRecordId;
        model.ModificationDetails.ModificationId = existingModificationId.ToString();
        SetupTempData(model);

        _modificationService
            .Setup(s => s.GetModification(projectRecordId, existingModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = null
            });

        // Act
        var result = await Sut.DuplicateModification();

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(404);

        _modificationService.Verify(s => s.GetModification(projectRecordId, existingModificationId), Times.Once);
        _modificationService.Verify(s => s.DuplicateModification(It.IsAny<DuplicateModificationRequest>()),
            Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task DuplicateModification_Get_ReturnsError_When_DuplicateModification_Fails
    (
        ReviewOutcomeViewModel model,
        string projectRecordId,
        Guid existingModificationId
    )
    {
        // Arrange
        model.ModificationDetails.ProjectRecordId = projectRecordId;
        model.ModificationDetails.ModificationId = existingModificationId.ToString();
        SetupTempData(model);

        _modificationService
            .Setup(s => s.GetModification(projectRecordId, existingModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationResponse
                {
                    Id = existingModificationId,
                    ProjectRecordId = projectRecordId
                }
            });

        _modificationService
            .Setup(s => s.DuplicateModification(It.IsAny<DuplicateModificationRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = null
            });

        // Act
        var result = await Sut.DuplicateModification();

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(404);

        _modificationService.Verify(s => s.GetModification(projectRecordId, existingModificationId), Times.Once);
        _modificationService.Verify(s => s.DuplicateModification(It.IsAny<DuplicateModificationRequest>()), Times.Once);

        // Should NOT set banners on failure
        Sut.TempData.ContainsKey(TempDataKeys.IsDuplicateModification).ShouldBeFalse();
        Sut.TempData.ContainsKey(TempDataKeys.ShowNotificationBanner).ShouldBeFalse();
    }

    private void SetupTempData(ReviewOutcomeViewModel model)
    {
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationsDetails] = JsonSerializer.Serialize(model)
        };
    }
}