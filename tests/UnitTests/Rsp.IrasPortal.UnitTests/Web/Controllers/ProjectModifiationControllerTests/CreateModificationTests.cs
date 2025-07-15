using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class CreateModification : TestServiceBase<ProjectModificationController>
{
    [Theory, AutoData]
    public async Task CreateModification_ReturnsProblem_WhenTempDataMissing
    (
        string separator
    )
    {
        // Arrange
        // No ProjectRecordId or IrasId in TempData
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.CreateModification(separator);

        // Assert
        var objResult = result.ShouldBeOfType<ObjectResult>();
        var problem = objResult.Value.ShouldBeOfType<ProblemDetails>();

        problem.Detail.ShouldNotBeNull();
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Detail.ShouldContain("ProjectRecordId and/or IrasId missing");
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
            [TempDataKeys.ProjectRecordId] = projectRecordId
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
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
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
            [TempDataKeys.ProjectRecordId] = projectRecordId
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
        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("qnc:resume");
        redirectResult.RouteValues.ShouldNotBeNull();
        redirectResult.RouteValues["projectRecordId"].ShouldBe(projectRecordId);
        redirectResult.RouteValues["categoryId"].ShouldBe(QuestionCategories.ProjectModification);
        Sut.TempData[TempDataKeys.ProjectModificationId].ShouldBe(modificationId);
        Sut.TempData[TempDataKeys.ProjectModificationIdentifier].ShouldBe(modificationIdentifier);
    }
}