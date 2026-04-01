using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Enum;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications;

public class CreateModification : TestServiceBase<ModificationsController>
{
    [Theory, AutoData]
    public async Task CreateModification_ReturnsProblem_WhenTempDataMissing
    (
        string separator
    )
    {
        // Arrange
        // No ProjectRecordId or IrasId in TempData
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.CanCreateNewModification] = ModificationCreationCheckResult.Success
        };
        Sut.TempData = tempData;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.CreateModification(separator);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Theory, AutoData]
    public async Task CreateModification_ReturnsServiceError_WhenServiceFails(
        string separator,
        string projectRecordId,
        int irasId
    )
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId,
            [TempDataKeys.ProjectModification.CanCreateNewModification] = ModificationCreationCheckResult.Success
        };

        // Simulate IrasId as int in TempData
        tempData[TempDataKeys.IrasId] = irasId;
        Sut.TempData = tempData;

        // Mock GetRespondentFromContext extension
        var respondent = new { GivenName = "John", FamilyName = "Doe" };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        Sut.HttpContext.Items["Respondent"] = respondent;

        // Mock service returns failure
        var serviceResponse = new ServiceResponse<ProjectModificationResponse>
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Error = "Service failed"
        };

        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.CreateModification(It.IsAny<ProjectModificationRequest>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.CreateModification(separator);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Theory, AutoData]
    public async Task CreateModification_RedirectsToResume_WhenSuccessful
    (
        string separator,
        string projectRecordId,
        int irasId,
        Guid modificationId,
        string modificationIdentifier
    )
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId,
            [TempDataKeys.ProjectModification.CanCreateNewModification] = ModificationCreationCheckResult.Success
        };

        tempData[TempDataKeys.IrasId] = irasId;

        Sut.TempData = tempData;

        // Mock GetRespondentFromContext extension
        var respondent = new { GivenName = "John", FamilyName = "Doe" };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        Sut.HttpContext.Items["Respondent"] = respondent;

        var modificationResponse = new ProjectModificationResponse
        {
            Id = modificationId,
            ProjectRecordId = projectRecordId,
            ModificationIdentifier = modificationIdentifier,
            Status = "OPEN",
            CreatedBy = "John Doe",
            UpdatedBy = "John Doe",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        var serviceResponse = new ServiceResponse<ProjectModificationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.CreateModification(It.IsAny<ProjectModificationRequest>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.CreateModification(separator);

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationId].ShouldBe(modificationId);
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier].ShouldBe(modificationIdentifier);
    }

    [Theory, AutoData]
    public async Task CreateModification_Validation_Error_When_CanCreateNewModification_Is_Invalid(
    string separator,
    int irasId)
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.CanCreateNewModification] = "INVALID_VALUE",
            [TempDataKeys.IrasId] = irasId
        };

        Sut.TempData = tempData;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        Sut.HttpContext.Items["Respondent"] = new { Id = "user1" };

        // Act
        var result = await Sut.CreateModification(separator);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("CreateModificationOutcome");
        redirect.RouteValues!["result"].ShouldBe(ModificationCreationCheckResult.InvalidStatus);
    }

    [Theory, AutoData]
    public async Task CreateModification_Allows_Creation_When_CanCreateNewModification_Is_Success(
    string separator,
    int irasId)
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.CanCreateNewModification] = nameof(ModificationCreationCheckResult.Success),
            [TempDataKeys.IrasId] = irasId,
            [TempDataKeys.ProjectRecordId] = "ABC123"
        };

        Sut.TempData = tempData;

        // Respondent mock
        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        Sut.HttpContext.Items["Respondent"] = new { Id = "userX" };

        // Fake valid service response
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.CreateModification(It.IsAny<ProjectModificationRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationResponse
                {
                    Id = Guid.NewGuid(),
                    ModificationIdentifier = "ABC/123",
                    ProjectRecordId = "ABC123"
                }
            });

        // Act
        var result = await Sut.CreateModification(separator);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("AreaOfChange");
    }

    [Fact]
    public async Task CreateModification_When_TempData_Missing_Should_Block_By_Default()
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        Sut.HttpContext.Items["Respondent"] = new { Id = "user1" };

        // Act
        var result = await Sut.CreateModification("/");

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("CreateModificationOutcome");
        redirect.RouteValues!["result"].ShouldBe(ModificationCreationCheckResult.InvalidStatus);
    }

    [Fact]
    public void CreateModificationOutcome_Returns_ViewResult_With_Expected_ViewPath()
    {
        // Arrange
        var result = Sut.CreateModificationOutcome(
            Rsp.IrasPortal.Application.Enum.ModificationCreationCheckResult.InvalidStatus
        );

        // Act
        var viewResult = result.ShouldBeOfType<ViewResult>();

        // Assert
        viewResult.ViewName.ShouldBe("CreateModificationOutcome");

        viewResult.Model
            .ShouldBe(Rsp.IrasPortal.Application.Enum.ModificationCreationCheckResult.InvalidStatus);
    }
}