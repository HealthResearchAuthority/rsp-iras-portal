using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Enums;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class AddDocumentDetailsListTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task AddDocumentDetailsList_WhenSpecificAreaOfChangeProvided_SetsPageTitle()
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
        Assert.Equal("Add document details for safety", model.PageTitle);
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
        Assert.Equal(string.Empty, model.PageTitle);
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
}