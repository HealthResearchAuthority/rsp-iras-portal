using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ReviewAllChangesControllerTests;

public class ReviewAllChanges_ServiceErrorCases : TestServiceBase<ReviewAllChangesController>
{
    [Fact]
    public async Task Returns_StatusCode_When_GetModification_Fails()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var modId = Guid.NewGuid();

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification("PR1", modId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ReviewAllChanges("PR1", "IRAS", "Short", modId);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Returns_BadRequest_When_No_Modification_Found()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var modId = Guid.NewGuid();

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification("PR1", modId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        // Act
        var result = await Sut.ReviewAllChanges("PR1", "IRAS", "Short", modId);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Returns_StatusCode_When_GetModificationChanges_Fails()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var modId = Guid.NewGuid();

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification("PR1", modId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationResponse
                {
                    Id = modId,
                    ModificationIdentifier = modId.ToString(),
                    Status = ModificationStatus.InDraft,
                    ProjectRecordId = "PR1",
                    ModificationNumber = 1,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    CreatedBy = "TestUser",
                    UpdatedBy = "TestUser"
                }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationChanges(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>
            {
                StatusCode = HttpStatusCode.BadGateway
            });

        // Act
        var result = await Sut.ReviewAllChanges("PR1", "IRAS", "Short", modId);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
    }

    [Fact]
    public async Task Returns_StatusCode_When_GetInitialModificationQuestions_Fails()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var modId = Guid.NewGuid();

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification("PR1", modId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationResponse
                {
                    Id = modId,
                    ModificationIdentifier = modId.ToString(),
                    Status = ModificationStatus.InDraft,
                    ProjectRecordId = "PR1",
                    ModificationNumber = 1,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    CreatedBy = "TestUser",
                    UpdatedBy = "TestUser"
                }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationChanges(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        var result = await Sut.ReviewAllChanges("PR1", "IRAS", "Short", modId);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }
}