using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Enums;
using Rsp.IrasPortal.Web.Features.Modifications.Documents.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Documents;

public class AddDocumentDetailsListTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public async Task AddDocumentDetailsList_WhenSpecificAreaOfChangeProvided_SetsPageTitle()
    {
        // Arrange
        var docId = Guid.NewGuid();

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentRequest>()
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    ActiveFrom = DateTime.UtcNow,
                    ActiveTo = DateTime.UtcNow.AddYears(1),
                    Id = "pdm-document-metadata",
                    Version = "1.0",
                    Sections = new List<SectionModel>()
                    {
                        new SectionModel
                        {
                            Id = "Q1",
                            Questions = new List<QuestionModel>()
                            {
                                new QuestionModel
                                {
                                    Id = "1",
                                    Version = "1.0",
                                    CategoryId = "cat1",
                                    SectionSequence = 1,
                                    Sequence = 1,
                                    ShortName = "Short Q1",
                                    AnswerDataType = "Dropdown",
                                    Conformance = "Mandatory",
                                    ShowOriginalAnswer = false,
                                    QuestionId = "Test",
                                    Name = "Test Question",
                                    QuestionFormat = "dropdown",
                                    Answers =
                                    [
                                        new() { Id = "opt1", OptionName = "Option 1" },
                                        new() { Id = "opt2", OptionName = "Option 2" }
                                    ],
                                    ValidationRules =
                                    [
                                        new RuleModel { Mode = "And", QuestionId = "Q1", Conditions = [new ConditionModel {OptionType= "M" } ]}
                                    ]
                                }
                            }
                        }
                    }
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>
                {
                    new ProjectModificationDocumentAnswerDto { QuestionId = "Test", AnswerText = "some text", OptionType = "dropdown", SelectedOption = "opt1" }
                }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.AddDocumentDetailsList();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationReviewDocumentsViewModel>(viewResult.Model);
        Assert.Equal("Safety", model.SpecificAreaOfChange);
    }

    [Fact]
    public async Task AddDocumentDetailsList_WhenDocumentsExist_ClonesQuestionnaireAndSetsDocumentStatus()
    {
        // Arrange
        var docWithAnswers = Guid.NewGuid();
        var docWithoutAnswers = Guid.NewGuid();

        // Mock: return two documents
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentRequest>
                {
                new() { Id = docWithAnswers, FileName = "doc1.pdf", FileSize = 123, DocumentStoragePath = "https://storage/doc1.pdf" },
                new() { Id = docWithoutAnswers, FileName = "doc2.pdf", FileSize = 456, DocumentStoragePath = "https://storage/doc2.pdf" }
                }
            });

        // Mock: CMS question set (needed for cloning)
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Id = "pdm-document-metadata",
                    Version = "1.0",
                    ActiveFrom = DateTime.UtcNow,
                    ActiveTo = DateTime.UtcNow.AddYears(1),
                    Sections =
                    [
                        new SectionModel
                    {
                        Id = "S1",
                        Questions =
                        [
                            new QuestionModel
                            {
                                Id = "Q1",
                                QuestionId = "Test",
                                Name = "Test Question",
                                Version = "1.0",
                                CategoryId = "cat",
                                SectionSequence = 1,
                                Sequence = 1,
                                ShortName = "Short Q",
                                AnswerDataType = "Dropdown",
                                Conformance = "Mandatory",
                                QuestionFormat = "dropdown",
                                Answers = [ new() { Id = "opt1", OptionName = "Option 1" } ]
                            }
                        ]
                    }
                    ]
                }
            });

        // Mock: answers for docWithAnswers (valid answers → Completed)
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docWithAnswers))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>
                {
                new() { QuestionId = "Test", AnswerText = "some text", OptionType = "dropdown", SelectedOption = "opt1" }
                }
            });

        // Mock: answers for docWithoutAnswers (empty answers → Incomplete)
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docWithoutAnswers))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>() // no answers
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.AddDocumentDetailsList();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationReviewDocumentsViewModel>(viewResult.Model);

        // Page title still correct
        Assert.Equal("Safety", model.SpecificAreaOfChange);

        // Two uploaded documents returned
        Assert.Equal(2, model.UploadedDocuments.Count);

        // doc1: has answers, should be Completed
        var doc1 = model.UploadedDocuments.Single(d => d.DocumentId == docWithAnswers);
        Assert.Equal(DocumentDetailStatus.Completed.ToString(), doc1.Status);

        // doc2: no answers, should be Incomplete
        var doc2 = model.UploadedDocuments.Single(d => d.DocumentId == docWithoutAnswers);
        Assert.Equal(DocumentDetailStatus.Incomplete.ToString(), doc2.Status);
    }

    [Fact]
    public async Task AddDocumentDetailsList_WhenNoDocuments_ReturnsEmptyList()
    {
        // Arrange
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentRequest>() // empty
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Mock: CMS question set (needed for cloning)
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Id = "pdm-document-metadata",
                    Version = "1.0",
                    ActiveFrom = DateTime.UtcNow,
                    ActiveTo = DateTime.UtcNow.AddYears(1),
                    Sections =
                    [
                        new SectionModel
                    {
                        Id = "S1",
                        Questions =
                        [
                            new QuestionModel
                            {
                                Id = "Q1",
                                QuestionId = "Test",
                                Name = "Test Question",
                                Version = "1.0",
                                CategoryId = "cat",
                                SectionSequence = 1,
                                Sequence = 1,
                                ShortName = "Short Q",
                                AnswerDataType = "Dropdown",
                                Conformance = "Mandatory",
                                QuestionFormat = "dropdown",
                                Answers = [ new() { Id = "opt1", OptionName = "Option 1" } ]
                            }
                        ]
                    }
                    ]
                }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.AddDocumentDetailsList();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationReviewDocumentsViewModel>(viewResult.Model);
        Assert.Empty(model.UploadedDocuments); // no documents returned
    }

    [Fact]
    public async Task AddDocumentDetailsList_WhenAnswersServiceFails_SetsDocumentAsIncomplete()
    {
        // Arrange
        var docId = Guid.NewGuid();

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentRequest>
                {
                new() { Id = docId, FileName = "doc.pdf", FileSize = 123, DocumentStoragePath = "https://storage/doc.pdf" }
                }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.InternalServerError, // simulate failure
                Content = null
            });

        // CMS question set required for cloning
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { Sections = new List<SectionModel>() }
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.AddDocumentDetailsList();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationReviewDocumentsViewModel>(viewResult.Model);

        // Document marked as incomplete
        var doc = model.UploadedDocuments.Single();
        Assert.Equal(DocumentDetailStatus.Incomplete.ToString(), doc.Status);
    }

    [Fact]
    public async Task AddDocumentDetailsList_WhenNoSpecificAreaOfChange_SetsEmptyPageTitle()
    {
        // Arrange
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentRequest>()
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse()
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = null,
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.AddDocumentDetailsList();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationReviewDocumentsViewModel>(viewResult.Model);
        Assert.Equal(string.Empty, model.SpecificAreaOfChange);
    }

    [Fact]
    public async Task AddDocumentDetailsList_WhenAnswersComplete_StatusIsCompleted()
    {
        // Arrange
        var docId = Guid.NewGuid();

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
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

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse()
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>
                {
                    new ProjectModificationDocumentAnswerDto { AnswerText = "some text", OptionType = "dropdown", SelectedOption = "opt1" }
                }
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        // Act
        var result = await Sut.AddDocumentDetailsList();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationReviewDocumentsViewModel>(viewResult.Model);
        Assert.Single(model.UploadedDocuments);
        Assert.Equal(DocumentDetailStatus.Completed.ToString(), model.UploadedDocuments[0].Status);
    }

    [Fact]
    public async Task AddDocumentDetailsList_WhenAnswersIncomplete_StatusIsIncomplete()
    {
        // Arrange
        var docId = Guid.NewGuid();
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
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

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse()
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>
                {
                    new ProjectModificationDocumentAnswerDto { AnswerText = "", OptionType = "", SelectedOption = "" }
                }
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult()
            {
                Errors = new List<ValidationFailure> {
                new("IrasId", "Required")
            }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.AddDocumentDetailsList();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationReviewDocumentsViewModel>(viewResult.Model);
        Assert.Single(model.UploadedDocuments);
        Assert.Equal(DocumentDetailStatus.Incomplete.ToString(), model.UploadedDocuments[0].Status);
    }

    [Fact]
    public async Task AddDocumentDetailsList_WhenServiceFails_UploadedDocumentsIsNull()
    {
        // Arrange
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = null
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse()
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.AddDocumentDetailsList();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationReviewDocumentsViewModel>(viewResult.Model);
        Assert.Empty(model.UploadedDocuments);
    }

    [Fact]
    public async Task PopulateAnswersFromDocuments_MatchingAnswerWithoutAnswers_UpdatesProperties()
    {
        var questionnaire = new QuestionnaireViewModel
        {
            Questions = new List<QuestionViewModel>
            {
                new() { QuestionId = "Q1", QuestionType = "Text" }
            }
        };

        var answers = new List<ProjectModificationDocumentAnswerDto>
        {
            new() { QuestionId = "Q1", AnswerText = "New", SelectedOption = "B", OptionType = null }
        };

        var result = await InvokePopulate(questionnaire, answers);

        var q = result.Questions[0];
        Assert.Equal("New", q.AnswerText);
        Assert.Equal("B", q.SelectedOption);
        Assert.Equal("Text", q.QuestionType); // unchanged since OptionType is null
        Assert.Empty(q.Answers);
    }

    [Fact]
    public async Task PopulateAnswersFromDocuments_MatchingAnswerWithOptionType_UpdatesQuestionType()
    {
        var questionnaire = new QuestionnaireViewModel
        {
            Questions = new List<QuestionViewModel>
            {
                new() { QuestionId = "Q1", QuestionType = "OldType" }
            }
        };

        var answers = new List<ProjectModificationDocumentAnswerDto>
        {
            new() { QuestionId = "Q1", AnswerText = "Ans", SelectedOption = "X", OptionType = "Radio" }
        };

        var result = await InvokePopulate(questionnaire, answers);

        var q = result.Questions[0];
        Assert.Equal("Radio", q.QuestionType); // updated from OptionType
    }

    [Fact]
    public async Task PopulateAnswersFromDocuments_MatchingAnswerWithAnswersList_MapsAnswerViewModels()
    {
        var questionnaire = new QuestionnaireViewModel
        {
            Questions = new List<QuestionViewModel>
            {
                new() { QuestionId = "Q1", QuestionType = "Multi" }
            }
        };

        var answers = new List<ProjectModificationDocumentAnswerDto>
        {
            new()
            {
                QuestionId = "Q1",
                AnswerText = "Ans",
                SelectedOption = "C",
                OptionType = "Checkbox",
                Answers = new List<string> { "Opt1", "Opt2" }
            }
        };

        var result = await InvokePopulate(questionnaire, answers);

        var q = result.Questions[0];
        Assert.Equal("Checkbox", q.QuestionType);
        Assert.Equal(2, q.Answers.Count);
        Assert.Contains(q.Answers, a => a.AnswerId == "Opt1" && a.IsSelected);
        Assert.Contains(q.Answers, a => a.AnswerId == "Opt2" && a.IsSelected);
    }

    private async Task<QuestionnaireViewModel> InvokePopulate(
        QuestionnaireViewModel questionnaire,
        IEnumerable<ProjectModificationDocumentAnswerDto> answers)
    {
        var method = Sut.GetType()
            .GetMethod("PopulateAnswersFromDocuments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return await (Task<QuestionnaireViewModel>)method.Invoke(Sut, new object[] { questionnaire, answers });
    }
}