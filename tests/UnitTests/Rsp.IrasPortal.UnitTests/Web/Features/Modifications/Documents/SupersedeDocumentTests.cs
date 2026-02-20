using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Documents.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.Documents;

public class SupersedeDocumentTests : TestServiceBase<DocumentsController>
{
    private void EnableSupersedeFeature()
    {
        Mocker.GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.SupersedingDocuments))
            .ReturnsAsync(true);
    }

    private void SetupCommonContext()
    {
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";
    }

    [Fact]
    public async Task SupersedeDocumentToReplace_WhenEligibleDocumentsExist_PopulatesDocumentToReplaceList()
    {
        // Arrange
        EnableSupersedeFeature();
        SetupCommonContext();

        var currentDocumentId = Guid.NewGuid();
        var eligibleDocId = Guid.NewGuid();

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(currentDocumentId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationDocumentRequest { Id = currentDocumentId }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentsByType("record-123", "type-1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentRequest>
                {
                    new()
                    {
                        Id = eligibleDocId,
                        Status = DocumentStatus.Approved,
                        DocumentType = "Clean",
                        LinkedDocumentId = null
                    },
                    new()
                    {
                        Id = currentDocumentId, // should be excluded
                        Status = DocumentStatus.Approved,
                        DocumentType = "Clean"
                    }
                }
            });

        // Act
        var result = await Sut.SupersedeDocumentToReplace("type-1", "record-123", currentDocumentId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(DocumentsController.SupersedeDocumentToReplace), viewResult.ViewName);

        var model = Assert.IsType<ModificationAddDocumentDetailsViewModel>(viewResult.Model);
        Assert.Single(model.DocumentToReplaceList);
    }

    [Fact]
    public async Task SupersedeDocumentToReplace_WhenNoEligibleDocuments_ReturnsEmptyList()
    {
        // Arrange
        EnableSupersedeFeature();
        SetupCommonContext();

        var currentDocumentId = Guid.NewGuid();

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(currentDocumentId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationDocumentRequest { Id = currentDocumentId }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentsByType(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentRequest>() // empty
            });

        // Act
        var result = await Sut.SupersedeDocumentToReplace("type-1", "record-123", currentDocumentId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationAddDocumentDetailsViewModel>(viewResult.Model);
        Assert.Empty(model.DocumentToReplaceList);
    }

    [Fact]
    public async Task SupersedeDocumentType_ReturnsViewWithBaseViewModel()
    {
        // Arrange
        EnableSupersedeFeature();
        SetupCommonContext();

        var currentDocumentId = Guid.NewGuid();

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(currentDocumentId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationDocumentRequest
                {
                    Id = currentDocumentId,
                    FileName = "doc.pdf"
                }
            });

        // Act
        var result = await Sut.SupersedeDocumentType("type-1", "record-123", currentDocumentId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(DocumentsController.SupersedeDocumentType), viewResult.ViewName);

        var model = Assert.IsType<ModificationAddDocumentDetailsViewModel>(viewResult.Model);
        Assert.Equal(currentDocumentId, model.DocumentId);
    }

    [Fact]
    public async Task SupersedeLinkDocument_WhenEligibleDocumentsExist_PopulatesDocumentList()
    {
        // Arrange
        EnableSupersedeFeature();
        SetupCommonContext();

        var currentDocumentId = Guid.NewGuid();
        var eligibleDocId = Guid.NewGuid();

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(currentDocumentId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationDocumentRequest { Id = currentDocumentId }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentRequest>
                {
                    new()
                    {
                        Id = eligibleDocId,
                        ReplacesDocumentId = null,
                        ReplacedByDocumentId = null,
                        LinkedDocumentId = null,
                        DocumentType = "Clean"
                    },
                    new()
                    {
                        Id = currentDocumentId // excluded
                    }
                }
            });

        // Act
        var result = await Sut.SupersedeLinkDocument("type-1", "record-123", currentDocumentId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(DocumentsController.SupersedeLinkDocument), viewResult.ViewName);

        var model = Assert.IsType<ModificationAddDocumentDetailsViewModel>(viewResult.Model);
        Assert.Single(model.DocumentToReplaceList);
    }

    [Fact]
    public async Task SaveSupersedeDocumentDetails_WhenContinueToDocumentType_RedirectsToSupersedeDocumentType()
    {
        // Arrange
        EnableSupersedeFeature();
        SetupCommonContext();

        var documentId = Guid.NewGuid();

        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = documentId,
            ProjectRecordId = "record-123",
            MetaDataDocumentTypeId = "type-1",
            ModificationId = Guid.NewGuid(),
            ReplacesDocumentId = Guid.NewGuid(),
            LinkedDocumentId = Guid.NewGuid(),
            DocumentType = SupersedeDocumentsType.Tracked
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(documentId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationDocumentRequest { Id = documentId }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationDocuments(It.IsAny<List<ProjectModificationDocumentRequest>>()))
            .ReturnsAsync(new ServiceResponse<bool> { StatusCode = HttpStatusCode.OK, Content = true });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>()
                {
                    new()
                    {
                        ModificationDocumentId = documentId,
                        QuestionId = QuestionIds.DocumentName,
                        AnswerText = "Answer 1",
                        SelectedOption = "Option 1",
                        OptionType = "SingleSelect",
                        Answers = new List<string> { "Option 1", "Option 2" },
                        CategoryId = "category-1",
                        SectionId = "section-1",
                        VersionId = "version-1",
                        Id = Guid.NewGuid()
                    }
                }
            });

        // Validator fails so we stay on the same view
        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveSupersedeDocumentDetails(
            viewModel,
            continueToDocumentType: true,
            linkDocument: true);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DocumentsController.AddDocumentDetailsList), redirect.ActionName);
    }

    [Fact]
    public async Task SaveSupersedeDocumentDetails_WhenSaveForLater_RedirectsToSaveForLater()
    {
        // Arrange
        EnableSupersedeFeature();
        SetupCommonContext();

        var documentId = Guid.NewGuid();

        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = documentId,
            ProjectRecordId = "record-123",
            ModificationId = Guid.NewGuid()
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(documentId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationDocumentRequest { Id = documentId }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationDocuments(It.IsAny<List<ProjectModificationDocumentRequest>>()))
            .ReturnsAsync(new ServiceResponse<bool> { StatusCode = HttpStatusCode.OK, Content = true });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        // Validator fails so we stay on the same view
        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveSupersedeDocumentDetails(
            viewModel,
            saveForLater: true);

        // Assert
        Assert.IsType<RedirectToRouteResult>(result);
    }

    [Fact]
    public async Task SaveSupersedeDocumentDetails_WithFileUpload_RedirectsToSupersedeDocumentType()
    {
        // Arrange
        EnableSupersedeFeature();
        SetupCommonContext();

        var documentId = Guid.NewGuid();

        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = documentId,
            ProjectRecordId = "record-123",
            MetaDataDocumentTypeId = "type-1",
            ModificationId = Guid.NewGuid(),
            ReplacesDocumentId = Guid.NewGuid(),
            LinkedDocumentId = Guid.NewGuid(),
            DocumentType = SupersedeDocumentsType.Tracked,
            File = new FormFile(new MemoryStream(), 0, 100, "file", "test.xyz")
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationDocumentRequest { Id = documentId }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationDocuments(It.IsAny<List<ProjectModificationDocumentRequest>>()))
            .ReturnsAsync(new ServiceResponse<bool> { StatusCode = HttpStatusCode.OK, Content = true });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>()
                {
                    new()
                    {
                        ModificationDocumentId = documentId,
                        QuestionId = QuestionIds.DocumentName,
                        AnswerText = "Answer 1",
                        SelectedOption = "Option 1",
                        OptionType = "SingleSelect",
                        Answers = new List<string> { "Option 1", "Option 2" },
                        CategoryId = "category-1",
                        SectionId = "section-1",
                        VersionId = "version-1",
                        Id = Guid.NewGuid()
                    }
                }
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        // Validator fails so we stay on the same view
        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveSupersedeDocumentDetails(
            viewModel,
            continueToDocumentType: true,
            linkDocument: true);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DocumentsController.AddDocumentDetailsList), redirect.ActionName);
    }
}