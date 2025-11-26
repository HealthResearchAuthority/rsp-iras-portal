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

public class ReviewDocumentTests : TestServiceBase<DocumentsController>
{
    [Theory, AutoData]
    public async Task Review_WithDocuments_ReturnsViewWithDocuments
    (
        string shortTitle,
        string irasId,
        string modificationIdentifier,
        string specificAreaOfChange,
        Guid changeId,
        string projectRecordId,
        string respondentId,
        List<ProjectModificationDocumentRequest> documentResponses
    )
    {
        // Arrange
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(changeId, projectRecordId, respondentId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = documentResponses
            });

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
        var result = await Sut.ModificationDocumentsAdded();

        // Assert
        result.ShouldBeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult!.ViewName.ShouldBeNull();

        var model = viewResult.Model.ShouldBeOfType<ModificationReviewDocumentsViewModel>();
        model.ShortTitle.ShouldBe(shortTitle);
        model.IrasId.ShouldBe(irasId);
        model.ModificationIdentifier.ShouldBe(modificationIdentifier);

        model.UploadedDocuments.Count.ShouldBe(documentResponses.Count);
    }

    [Theory, AutoData]
    public async Task Review_ResponseFails_AddsModelErrorAndReturnsEmptyDocuments(
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
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(changeId, projectRecordId, respondentId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = null
            });

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
        var result = await Sut.ModificationDocumentsAdded();

        // Assert
        result.ShouldBeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult!.ViewName.ShouldBeNull();

        var model = viewResult.Model.ShouldBeOfType<ModificationReviewDocumentsViewModel>();
        model.UploadedDocuments.ShouldBeEmpty();
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState[string.Empty].Errors.ShouldContain(e =>
            e.ErrorMessage == "No documents found or an error occurred while retrieving documents");
    }
}