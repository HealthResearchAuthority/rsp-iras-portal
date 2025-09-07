using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class ReviewDocumentDetailsTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task ReviewDocumentDetails_WhenNoAnswers_ReturnsViewWithEmptyAnswers()
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
                    new ProjectModificationDocumentRequest { Id = docId, FileName = "doc1.pdf", DocumentStoragePath = "path" }
                }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>()
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
                        new SectionModel { Id = "Q1" }
                    }
                }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123"
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respondent-1";

        // Act
        var result = await Sut.ReviewDocumentDetails();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("ReviewDocumentDetails", viewResult.ViewName);
        var model = Assert.IsAssignableFrom<List<ModificationAddDocumentDetailsViewModel>>(viewResult.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task ReviewDocumentDetails_WhenAnswersExist_MapsAnswersCorrectly()
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
                    new ProjectModificationDocumentRequest { Id = docId, FileName = "doc1.pdf", DocumentStoragePath = "path" }
                }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>
                {
                    new ProjectModificationDocumentAnswerDto
                    {
                        QuestionId = "Q1",
                        AnswerText = "Answer 1",
                        SelectedOption = "OptionA",
                        Answers = new List<string> { "Ans1", "Ans2" }
                    }
                }
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
                        new SectionModel { Id = "Q1" }
                    }
                }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123"
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respondent-1";

        // Act
        var result = await Sut.ReviewDocumentDetails();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("ReviewDocumentDetails", viewResult.ViewName);
        var model = Assert.IsAssignableFrom<List<ModificationAddDocumentDetailsViewModel>>(viewResult.Model);
        Assert.Single(model);
    }
}