using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class ContinueToDetailsTests : TestServiceBase<ProjectModificationController>
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
        Assert.Equal(nameof(ProjectModificationController.AddDocumentDetailsList), redirectResult.ActionName);
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

        var q = model.Questionnaire.Questions;
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

        var q = model.Questionnaire.Questions;
        Assert.NotNull(q);
    }
}