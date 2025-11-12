using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.Documents.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Documents;

public class ConfirmDeleteDocumentTests : TestServiceBase<DocumentsController>
{
    [Theory, AutoData]
    public async Task ConfirmDeleteDocument_Should_Return_View_With_ViewModel
    (
        Guid id,
        string backRoute,
        string shortTitle,
        string irasId,
        string modificationIdentifier,
        string specificAreaOfChange,
        Guid changeId,
        string projectRecordId,
        string respondentId,
        ProjectModificationDocumentRequest documentDto)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<ProjectModificationDocumentRequest>
        {
            Content = documentDto,
            StatusCode = HttpStatusCode.OK
        };

        var respondentServiceMock = Mocker.GetMock<IRespondentService>();

        respondentServiceMock
            .Setup(s => s.GetModificationDocumentDetails(id))
            .ReturnsAsync(serviceResponse);

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = shortTitle,
            [TempDataKeys.IrasId] = irasId,
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationIdentifier,
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = specificAreaOfChange,
            [TempDataKeys.ProjectModification.ProjectModificationId] = changeId,
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { [ContextItemKeys.RespondentId] = respondentId }
            }
        };

        // Act
        var result = await Sut.ConfirmDeleteDocument(id, backRoute);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("DeleteDocuments");

        var model = viewResult.Model.ShouldBeOfType<ModificationDeleteDocumentViewModel>();
        model.BackRoute.ShouldBe(backRoute);
        model.Documents.ShouldNotBeNull();
        model.Documents.Count.ShouldBe(1);

        var request = model.Documents.First();
        request.Id.ShouldBe(id);
        request.FileName.ShouldBe(documentDto.FileName);
        request.FileSize.ShouldBe(documentDto.FileSize ?? 0);
        request.DocumentStoragePath.ShouldBe(documentDto.DocumentStoragePath);

        // Verify service was called
        respondentServiceMock.Verify(s => s.GetModificationDocumentDetails(id), Times.Once);
    }

    [Theory, AutoData]
    public async Task ConfirmDeleteDocument_Should_Handle_Null_DocumentDetails
    (
        Guid id,
        string shortTitle,
        string irasId,
        string modificationIdentifier,
        string specificAreaOfChange,
        Guid changeId,
        string projectRecordId,
        string respondentId,
        string backRoute
    )
    {
        // Arrange
        var respondentServiceMock = Mocker.GetMock<IRespondentService>();

        respondentServiceMock
            .Setup(s => s.GetModificationDocumentDetails(id))
            .ReturnsAsync((ServiceResponse<ProjectModificationDocumentRequest>)null);

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = shortTitle,
            [TempDataKeys.IrasId] = irasId,
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationIdentifier,
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = specificAreaOfChange,
            [TempDataKeys.ProjectModification.ProjectModificationId] = changeId,
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { [ContextItemKeys.RespondentId] = respondentId }
            }
        };

        // Act
        var result = await Sut.ConfirmDeleteDocument(id, backRoute);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("DeleteDocuments");

        var model = viewResult.Model.ShouldBeOfType<ModificationDeleteDocumentViewModel>();
        model.BackRoute.ShouldBe(backRoute);
        model.Documents.ShouldNotBeNull();
        model.Documents.Count.ShouldBe(1);

        var request = model.Documents.First();
        request.Id.ShouldBe(id);
        request.FileName.ShouldBeNull();
        request.FileSize.ShouldBe(0);
        request.DocumentStoragePath.ShouldBeNull();

        // Verify service was called
        respondentServiceMock.Verify(s => s.GetModificationDocumentDetails(id), Times.Once);
    }
}