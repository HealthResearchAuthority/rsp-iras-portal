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

public class ConfirmDeleteDocumentsTests : TestServiceBase<DocumentsController>
{
    [Theory, AutoData]
    public async Task ConfirmDeleteDocuments_Should_Return_View_With_ViewModel
    (
        string shortTitle,
        string irasId,
        string modificationIdentifier,
        string specificAreaOfChange,
        Guid changeId,
        string projectRecordId,
        string respondentId,
        IEnumerable<ProjectModificationDocumentRequest> documents
    )
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
        {
            Content = documents,
            StatusCode = HttpStatusCode.OK
        };

        var respondentServiceMock = Mocker.GetMock<IRespondentService>();

        respondentServiceMock
            .Setup(s => s.GetModificationChangesDocuments(changeId, projectRecordId, respondentId))
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
        var result = await Sut.ConfirmDeleteDocuments(It.IsAny<string>());

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("DeleteDocuments");

        var model = viewResult.Model.ShouldBeOfType<ModificationDeleteDocumentViewModel>();
        model.Documents.ShouldNotBeNull();
        model.Documents.Count.ShouldBe(documents.ToList().Count);

        // Verify first document is mapped correctly
        var expected = documents.OrderBy(dto => dto.FileName, StringComparer.OrdinalIgnoreCase).First();
        var actual = model.Documents.OrderBy(dto => dto.FileName, StringComparer.OrdinalIgnoreCase).First();
        actual.Id.ShouldBe(expected.Id);
        actual.FileName.ShouldBe(expected.FileName);
        actual.FileSize.ShouldBe(expected.FileSize);
        actual.DocumentStoragePath.ShouldBe(expected.DocumentStoragePath);
        actual.ProjectModificationId.ShouldBe(changeId);
        actual.ProjectRecordId.ShouldBe(projectRecordId);
        actual.ProjectPersonnelId.ShouldBe(respondentId);

        respondentServiceMock.Verify(s => s.GetModificationChangesDocuments(changeId, projectRecordId, respondentId), Times.Once);
    }

    [Theory, AutoData]
    public async Task ConfirmDeleteDocuments_Should_Redirect_When_Service_Fails
    (
        string shortTitle,
        string irasId,
        string modificationIdentifier,
        string specificAreaOfChange,
        Guid changeId,
        string projectRecordId,
        string respondentId
    )
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
        {
            Content = null,
            StatusCode = HttpStatusCode.BadRequest
        };

        var respondentServiceMock = Mocker.GetMock<IRespondentService>();

        respondentServiceMock
            .Setup(s => s.GetModificationChangesDocuments(changeId, projectRecordId, respondentId))
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
        var result = await Sut.ConfirmDeleteDocuments(It.IsAny<string>());

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(DocumentsController.ProjectDocument));

        respondentServiceMock.Verify(s => s.GetModificationChangesDocuments(changeId, projectRecordId, respondentId), Times.Once);
    }
}