using System.Text.Json;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

public class DisplayQuestionnaireTests : TestServiceBase<QuestionnaireController>
{
    [Theory]
    [AutoData]
    public async Task
        DisplayQuestionnaire_ShouldReturnViewWithQuestionnaireFromSession_WhenSessionContainsValidQuestionnaireData
        (
            string categoryId,
            string sectionId,
            List<QuestionsResponse> questionsResponse,
            List<QuestionSectionsResponse> questionSectionsResponse
        )
    {
        var faker = new Faker<QuestionViewModel>();
        List<QuestionViewModel> expectedQuestions = faker.Generate(3);

        // Arrange
        var response = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionsResponse
        };

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestions(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

        var session = new Mock<ISession>();
        session
            .Setup(s => s.Keys)
            .Returns([SessionKeys.Questionnaire]);

        session
            .Setup(s => s.TryGetValue(SessionKeys.Questionnaire, out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (key != SessionKeys.Questionnaire)
                {
                    value = null;
                    return false;
                }

                value = JsonSerializer.SerializeToUtf8Bytes(expectedQuestions);
                return true;
            });

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.CurrentStage] = categoryId
        };

        // Act
        var result = await Sut.DisplayQuestionnaire(categoryId, sectionId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");
        viewResult.Model.ShouldBeOfType<QuestionnaireViewModel>();

        Mocker
            .GetMock<IQuestionSetService>()
            .Verify(s => s.GetQuestions(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task DisplayQuestionnaire_ShouldReturnErrorView_WhenGetQuestionsReturnsErrorResponse
    (
        string categoryId,
        string sectionId,
        List<QuestionSectionsResponse> questionSectionsResponse
    )
    {
        // Arrange
        var response = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions(It.IsAny<string>()))
            .ReturnsAsync(response);

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = questionSectionsResponse
        };

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        var session = new Mock<ISession>();

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.DisplayQuestionnaire(categoryId, sectionId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
    }

    [Theory]
    [AutoData]
    public async Task
        DisplayQuestionnaire_ShouldReturnViewWithQuestionnaire_WhenSessionIsEmptyAndGetQuestionsReturnsValidQuestions
        (
            string categoryId,
            string sectionId,
            List<QuestionsResponse> questionsResponse,
            List<QuestionSectionsResponse> questionSectionsResponse
        )
    {
        // Arrange
        var response = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionsResponse
        };

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions(It.IsAny<string>()))
            .ReturnsAsync(response);

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestions(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

        var session = new Mock<ISession>();
        session
            .Setup(s => s.Keys)
            .Returns([]);

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.CurrentStage] = categoryId
        };

        var questions = response.Content
            .OrderBy(q => q.SectionId)
            .ThenBy(q => q.Sequence)
            .Select((question, index) => (question, index));

        var expectedQuestionnaire = new QuestionnaireViewModel
        {
            CurrentStage = categoryId,
            Questions = questions.Select(q => new QuestionViewModel
            {
                Index = q.index,
                QuestionId = q.question.QuestionId,
                VersionId = q.question.VersionId ?? string.Empty,
                Category = q.question.Category,
                SectionId = q.question.SectionId,
                Section = q.question.Section,
                Sequence = q.question.Sequence,
                Heading = q.question.Heading,
                QuestionText = q.question.QuestionText,
                ShortQuestionText = q.question.ShortQuestionText,
                QuestionType = q.question.QuestionType,
                DataType = q.question.DataType,
                IsMandatory = q.question.IsMandatory,
                IsOptional = q.question.IsOptional,
                Rules = q.question.Rules,
                Answers = q.question.Answers.Select(ans => new AnswerViewModel
                {
                    AnswerId = ans.AnswerId,
                    AnswerText = ans.AnswerText
                }).ToList()
            }).ToList()
        };

        // Act
        var result = await Sut.DisplayQuestionnaire(categoryId, sectionId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");

        var model = viewResult.Model.ShouldBeOfType<QuestionnaireViewModel>();
        model.Questions.ShouldBeEquivalentTo(expectedQuestionnaire.Questions);

        Mocker
            .GetMock<IQuestionSetService>()
            .Verify(s => s.GetQuestions(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task Resume_Should_Filter_Questions_For_ModificationJourney
    (
        string applicationId,
        string categoryId,
        string sectionId,
        Guid modificationId,
        Guid modificationChangeId
    )
    {
        // Arrange
        // Set TempData to simulate modification journey
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = modificationId,
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChangeId
        };

        Sut.TempData = tempData;

        var httpContext = new DefaultHttpContext();
        var session = new Mock<ISession>();
        httpContext.Session = session.Object;
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse()
            });

        // Only GetModificationAnswers should be called
        var respondentAnswers = new List<RespondentAnswerDto>();
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationAnswers(modificationChangeId, categoryId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = respondentAnswers
            });

        // Provide a mix of modification and non-modification questions
        var questions = new List<QuestionsResponse>
        {
            new() { QuestionId = "1", IsModificationQuestion = true },
            new() { QuestionId = "2", IsModificationQuestion = false }
        };

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions(categoryId, sectionId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = questions
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse()
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse()
            });

        // Act
        var result = await Sut.Resume(applicationId, categoryId, "False", sectionId);

        // Assert
        // Only the modification question should be present in the questionnaire
        session.Verify(s => s.Set(It.Is<string>(k => k.Contains(sectionId)), It.IsAny<byte[]>()), Times.Once);

        Mocker
            .GetMock<IRespondentService>()
            .Verify(s => s.GetModificationAnswers(modificationChangeId, categoryId), Times.Once);
        Mocker
            .GetMock<IRespondentService>()
            .Verify(s => s.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task Resume_Should_Not_Filter_Questions_When_Not_ModificationJourney
    (
        string applicationId,
        string categoryId,
        string sectionId
    )
    {
        // Arrange
        // No modification keys in TempData
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        var httpContext = new DefaultHttpContext();
        var session = new Mock<ISession>();
        httpContext.Session = session.Object;
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse()
            });

        var respondentAnswers = new List<RespondentAnswerDto>();
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = respondentAnswers
            });

        var questions = new List<QuestionsResponse>
        {
            new() { QuestionId = "1", IsModificationQuestion = true },
            new() { QuestionId = "2", IsModificationQuestion = false }
        };

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions(categoryId, sectionId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = questions
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse()
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse()
            });

        // Act
        var result = await Sut.Resume(applicationId, categoryId, "False", sectionId);

        // Assert
        // Both questions should be present in the questionnaire
        session.Verify(s => s.Set(It.Is<string>(k => k.Contains(sectionId)), It.IsAny<byte[]>()), Times.Once);

        Mocker
            .GetMock<IRespondentService>()
            .Verify(s => s.GetRespondentAnswers(applicationId, categoryId), Times.Once);
        Mocker
            .GetMock<IRespondentService>()
            .Verify(s => s.GetModificationAnswers(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }
}