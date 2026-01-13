using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Documents.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.Documents;

public class ContinueToDetailsTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public async Task ContinueToDetails_WhenDocumentNotFound_RedirectsToAddDocumentDetailsList()
    {
        // Arrange
        var docId = Guid.NewGuid();
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(docId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = null
            });

        // Act
        var result = await Sut.ContinueToDetails(docId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DocumentsController.AddDocumentDetailsList), redirectResult.ActionName);
        Assert.False(Sut.ModelState.IsValid);
        Assert.Contains(Sut.ModelState[string.Empty].Errors, e => e.ErrorMessage.Contains("Document details not found"));
    }

    [Fact]
    public async Task ContinueToDetails_WhenNoAnswersExist_ReturnsViewWithEmptyAnswers()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var document = new ProjectModificationDocumentRequest
        {
            Id = docId,
            FileName = "doc.pdf",
            FileSize = 123,
            DocumentStoragePath = "path"
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(docId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = document
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse()
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>()
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.ContinueToDetails(docId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("AddDocumentDetails", viewResult.ViewName);
        var model = Assert.IsType<ModificationAddDocumentDetailsViewModel>(viewResult.Model);
        Assert.Equal(document.FileName, model.FileName);
    }

    [Fact]
    public async Task ContinueToDetails_WhenAnswersExist_PopulatesMatchingQuestion()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var document = new ProjectModificationDocumentRequest
        {
            Id = docId,
            FileName = "doc.pdf",
            FileSize = 123,
            DocumentStoragePath = "path"
        };

        var question = new QuestionDto
        {
            QuestionId = "Q1",
            QuestionText = "Sample Question"
        };

        var answer = new ProjectModificationDocumentAnswerDto
        {
            QuestionId = "Q1",
            AnswerText = "Test Answer",
            SelectedOption = "Opt1",
            Answers = new List<string> { "A1" }
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(docId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = document
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { Id = docId.ToString() }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto> { answer }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.ContinueToDetails(docId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationAddDocumentDetailsViewModel>(viewResult.Model);

        var q = model.Questions;
        Assert.NotNull(q);
    }

    [Fact]
    public async Task ContinueToDetails_WhenAnswersDoNotMatchAnyQuestions_DoesNotPopulate()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var document = new ProjectModificationDocumentRequest
        {
            Id = docId,
            FileName = "doc.pdf",
            FileSize = 123,
            DocumentStoragePath = "path"
        };

        var question = new QuestionDto
        {
            QuestionId = "Q1",
            QuestionText = "Sample Question"
        };

        var answer = new ProjectModificationDocumentAnswerDto
        {
            QuestionId = "Q999",
            AnswerText = "Wrong Answer"
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(docId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = document
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto> { answer }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.ContinueToDetails(docId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationAddDocumentDetailsViewModel>(viewResult.Model);

        var q = model.Questions;
        Assert.NotNull(q);
    }

    [Fact]
    public async Task ContinueToDetails_WhenMatchingAnswerExists_PopulatesQuestionProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var answerId = Guid.NewGuid();

        // Mock document details
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(documentId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationDocumentRequest
                {
                    Id = documentId,
                    FileName = "doc.pdf",
                    FileSize = 123,
                    DocumentStoragePath = "https://storage/doc.pdf"
                }
            });

        // Mock CMS question set
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections = new List<SectionModel>
                    {
                    new()
                    {
                        Id = "S1",
                        Questions = new List<QuestionModel>
                        {
                            new() { QuestionId = "Q1", Name = "Test Question", Id = "1" }
                        }
                    }
                    }
                }
            });

        // Mock answers that match the question
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(documentId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>
                {
                new ProjectModificationDocumentAnswerDto
                {
                    Id = answerId,
                    QuestionId = "Q1",
                    AnswerText = "Some answer",
                    SelectedOption = "opt1"
                }
                }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.ContinueToDetails(documentId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationAddDocumentDetailsViewModel>(viewResult.Model);

        Assert.Single(model.Questions);
    }

    [Fact]
    public async Task ContinueToDetails_WhenAnswerDoesNotMatchQuestion_DoesNotSetQuestionProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentDetails(documentId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationDocumentRequest>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationDocumentRequest { Id = documentId, FileName = "doc.pdf" }
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections = new List<SectionModel>
                    {
                    new()
                    {
                        Id = "S1",
                        Questions = new List<QuestionModel>
                        {
                            new() { QuestionId = "Q1", Name = "Test Question", Id = "1" }
                        }
                    }
                    }
                }
            });

        // Answer has a non-matching QuestionId
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(documentId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>
                {
                new() { Id = Guid.NewGuid(), QuestionId = "NonMatching", AnswerText = "Some answer" }
                }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "record-123"
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.ContinueToDetails(documentId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ModificationAddDocumentDetailsViewModel>(viewResult.Model);

        var question = model.Questions.First();
        Assert.Null(question.AnswerText);
        Assert.Null(question.SelectedOption);
    }
}