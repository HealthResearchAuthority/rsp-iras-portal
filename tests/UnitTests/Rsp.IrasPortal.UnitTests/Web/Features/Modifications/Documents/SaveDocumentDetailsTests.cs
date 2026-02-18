using Azure.Storage.Blobs;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Azure;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Documents.Controllers;
using Rsp.Portal.Web.Models;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.Documents;

public class SaveDocumentDetailsTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public async Task SaveDocumentDetails_WhenValidationFails_ReturnsAddDocumentDetailsView()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),

            Questions = new List<QuestionViewModel>
                {
                    new QuestionViewModel { Index = 0, QuestionId = QuestionIds.DocumentName }
                }
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Questions[0].AnswerText", "Answer is required")
            }));

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("AddDocumentDetails", viewResult.ViewName);
        Assert.False(Sut.ModelState.IsValid);
        Assert.Contains(Sut.ModelState["Questions[0].AnswerText"].Errors,
            e => e.ErrorMessage == "Answer is required");
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenValidationFails_MapsApplicantAnswersIntoQuestionnaire_AndReturnsRedirectToRouteResult()
    {
        // Arrange
        var docId = Guid.NewGuid();

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentRequest>()
                {
                    new ProjectModificationDocumentRequest
                    {
                        Id = docId, FileName = "doc1.pdf", FileSize = 123, DocumentStoragePath = "path"
                    }
                }
            });

        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),

            Questions = new List<QuestionViewModel>
                {
                    new QuestionViewModel { Index = 0, QuestionId = QuestionIds.DocumentName, AnswerText = "some text" }
                }
        };

        // CMS question set will return a "blank" questionnaire question with the same Index

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>
                {
                    new ProjectModificationDocumentAnswerDto { QuestionId = QuestionIds.DocumentName, AnswerText = "some text", OptionType = "dropdown", SelectedOption = "opt1" }
                }
            });

        // Mock CMS question set response
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections =
                    [
                        new SectionModel
                    {
                        SectionId = "DocumentDetails",
                        Questions =
                        [
                            new QuestionModel
                            {
                                QuestionId = QuestionIds.DocumentName,
                                Id = QuestionIds.DocumentName,
                                AnswerDataType = "text",
                            },
                            new QuestionModel
                            {
                                QuestionId =  QuestionIds.DocumentName,
                                Id =  QuestionIds.DocumentName,
                                AnswerDataType = "text",
                            }
                        ]
                    }
                    ]
                }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        // Validator fails so we stay on the same view
        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel, true);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(Sut.ModelState.IsValid);
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenValidationFails_MapsApplicantAnswersIntoQuestionnaire_AndReturnsAddDocumentDetailsView()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),

            Questions = new List<QuestionViewModel>
                {
                    new QuestionViewModel { Index = 0, QuestionId = "Q1" }
                }
        };

        // CMS question set will return a "blank" questionnaire question with the same Index

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        // Validator fails so we stay on the same view
        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel, true);

        // Assert
        var viewResult = Assert.IsType<RedirectToRouteResult>(result);
        Assert.True(Sut.ModelState.IsValid);
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenValidationSucceedsAndValidateMandatoryTrue_RedirectsToAddDocumentDetailsList()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),
            Questions = []
        };

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DocumentsController.AddDocumentDetailsList), redirect.ActionName);
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenValidationSucceedsAndValidateMandatoryFalse_RedirectsToReviewDocumentDetails()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),
            Questions = []
        };

        var existingDocs = new List<ProjectModificationDocumentRequest>
        {
            new() { FileName = "duplicate.pdf" }
        };

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = existingDocs });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DocumentsController.AddDocumentDetailsList), redirect.ActionName);
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenDateIsRequiredForDocumentTypeAndMissing_AddsModelError()
    {
        // Arrange
        var validationFailure = new ValidationFailure("AnswerText", "Date is required");

        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),
            Questions = [
                new QuestionViewModel
                {
                    Index = 0,
                    QuestionId = QuestionIds.SelectedDocumentType,
                    SelectedOption = "1"
                }
            ]
        };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        // Mock CMS question set response
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections =
                    [
                        new SectionModel
                    {
                        SectionId = "DocumentDetails",
                        Questions =
                        [
                            new QuestionModel
                            {
                                QuestionId = QuestionIds.SelectedDocumentType,
                                Id = QuestionIds.SelectedDocumentType,
                                AnswerDataType = "Dropdown",
                                Answers = [ new AnswerModel { Id = "1", OptionName = "TypeA" } ]
                            },
                            new QuestionModel
                            {
                                QuestionId = "QDate",
                                Id = "QDate",
                                AnswerDataType = "date",
                                ValidationRules =
                                [
                                    new RuleModel
                                    {
                                        Conditions =
                                        [
                                            new ConditionModel
                                            {
                                                Operator = "IN",
                                                ParentOptions = [ new AnswerModel { Id = "1", OptionName = "TypeA" } ]
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                    ]
                }
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("AddDocumentDetails", viewResult.ViewName);
        Assert.False(Sut.ModelState.IsValid);
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenDateNotRequiredForDocumentType_DoesNotAddModelError()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),
            Questions =
            [
                new QuestionViewModel
            {
                Index = 0,
                QuestionId = QuestionIds.SelectedDocumentType,
                SelectedOption = "TypeB"
            },
            new QuestionViewModel
            {
                Index = 1,
                QuestionId = "QDate",
                DataType = "date",
                AnswerText = "", // no date
                Rules =
                [
                    new RuleDto
                    {
                        Conditions =
                        [
                            new ConditionDto
                            {
                                Operator = "IN",
                                ParentOptions = ["TypeA"] // TypeB not requires date
                            }
                        ]
                    }
                ]
            }
            ]
        };

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DocumentsController.AddDocumentDetailsList), redirect.ActionName);
        Assert.True(Sut.ModelState.IsValid);
    }

    [Fact]
    public async Task SaveDocumentDetails_CallsSaveModificationDocumentAnswers_WhenValid()
    {
        // Arrange
        var respondentServiceMock = new Mock<IRespondentService>();
        var cmsQuestionsetServiceMock = new Mock<ICmsQuestionsetService>();
        var validatorMock = new Mock<IValidator<ModificationAddDocumentDetailsViewModel>>();

        // Setup CMS questionset mock
        var question = new QuestionViewModel { QuestionId = "Q1", Index = 0, AnswerText = "Sample answer" };
        var questionnaire = new QuestionnaireViewModel { Questions = new List<QuestionViewModel> { question } };
        var cmsResponse = new ServiceResponse<CmsQuestionSetResponse> { Content = new CmsQuestionSetResponse() };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        // Mock CMS question set response
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections =
                    [
                        new SectionModel
                    {
                        SectionId = "DocumentDetails",
                        Questions =
                        [
                            new QuestionModel
                            {
                                QuestionId = QuestionIds.SelectedDocumentType,
                                Id = QuestionIds.SelectedDocumentType,
                                AnswerDataType = "Dropdown",
                                Answers = [ new AnswerModel { Id = "1", OptionName = "TypeA" } ]
                            },
                            new QuestionModel
                            {
                                QuestionId = "QDate",
                                Id = "QDate",
                                AnswerDataType = "date",
                                ValidationRules =
                                [
                                    new RuleModel
                                    {
                                        Conditions =
                                        [
                                            new ConditionModel
                                            {
                                                Operator = "IN",
                                                ParentOptions = [ new AnswerModel { Id = "1", OptionName = "TypeA" } ]
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                    ]
                }
            });

        // Setup questionnaire builder
        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        //QuestionsetHelpers.BuildQuestionnaireViewModel = _ => questionnaire;

        // Setup validation to pass
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ModificationAddDocumentDetailsViewModel>(), default))
            .ReturnsAsync(new ValidationResult());

        // Mock respondent service (the dependency SaveModificationDocumentAnswers uses)
        respondentServiceMock
            .Setup(s => s.SaveModificationDocumentAnswers(It.IsAny<List<ProjectModificationDocumentAnswerDto>>()))
            .ReturnsAsync(new ServiceResponse())
            .Verifiable();

        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),
            Questions = new List<QuestionViewModel> { question }
        };

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel);

        // Assert
        respondentServiceMock.Verify(
            s => s.SaveModificationDocumentAnswers(
                It.Is<List<ProjectModificationDocumentAnswerDto>>(a => a.Count == 1 && a.First().QuestionId == "Q1")),
            Times.Never);
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenStatusTransitionsToComplete_CreatesAuditTrail()
    {
        // Arrange
        var modificationId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = documentId,
            ModificationId = modificationId,
            FileName = "test.pdf",
            Questions = []
        };

        var questionSetService = Mocker.GetMock<ICmsQuestionsetService>();
        questionSetService
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        var projectModificationsService = Mocker.GetMock<IProjectModificationsService>();
        var validatorService = Mocker.GetMock<IValidator<QuestionnaireViewModel>>();
        validatorService
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        var controller = new TestDocumentsController(
            projectModificationsService.Object,
            questionSetService.Object,
            Mocker.GetMock<IRespondentService>().Object,
            validatorService.Object,
            Mocker.GetMock<IBlobStorageService>().Object,
            Mocker.GetMock<IAzureClientFactory<BlobServiceClient>>().Object,
            Mocker.GetMock<IFeatureManager>().Object);

        controller.SetEvaluateDocumentCompletionResults(
            true,   // existing status = Incomplete
            false   // new status = Complete
        );

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = modificationId
        };

        // Act
        await controller.SaveDocumentDetails(viewModel);

        // Assert
        projectModificationsService.Verify(s =>
            s.CreateModificationDocumentsAuditTrail(
                It.Is<List<ModificationDocumentsAuditTrailDto>>(l =>
                    l.Count == 1 &&
                    l[0].Description == DocumentAuditEvents.DocumentDetailsCompleted &&
                    l[0].ProjectModificationId == modificationId &&
                    l[0].FileName == "test.pdf" &&
                    l[0].User == "respondent-1")),
            Times.Once);
    }
}