using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.UnitTests;
using Rsp.Portal.Web.Features.Modifications.Documents.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Documents;

public class GetAuditHistoryTests : TestServiceBase<DocumentsController>
{
    [Theory, AutoData]
    public async Task GetAuditHistory_ReturnsView_WithCorrectViewModel(
        string shortTitle,
        int? irasId,
        string projectRecordId,
        string projectModificationIdentifier,
        string projectModificationId,
        ProjectDocumentsAuditTrailResponse projectDocumentsAuditTrailResponse
    )
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId,
            [TempDataKeys.ShortProjectTitle] = shortTitle,
            [TempDataKeys.IrasId] = irasId,
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = projectModificationIdentifier,
            [TempDataKeys.ProjectModification.ProjectModificationId] = projectModificationId
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetProjectDocumentsAuditTrail(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<ProjectDocumentsAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = projectDocumentsAuditTrailResponse
            });

        // Act
        var result = await Sut.GetAuditHistory
        (
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        );

        // Assert
        result.ShouldBeOfType<ViewResult>();
        var viewResult = result as ViewResult;

        viewResult!.ViewName.ShouldBe("DocumentAuditHistory");
        viewResult.Model.ShouldBeOfType<ProjectDocumentsAuditTrailViewModel>();

        var model = viewResult.Model as ProjectDocumentsAuditTrailViewModel;
        model!.ProjectOverviewModel!.ProjectTitle.ShouldBe(shortTitle);
        model!.ProjectOverviewModel!.IrasId.ShouldBe(irasId);
    }

    [Fact]
    public async Task GetAuditHistory_ReturnsView_WithEmptyFallbackValues()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.IrasId] = null,
            [TempDataKeys.ShortProjectTitle] = null,
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = null
        };

        Sut.TempData = tempData;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetProjectDocumentsAuditTrail(It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<ProjectDocumentsAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.GetAuditHistory
        (
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        );

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}