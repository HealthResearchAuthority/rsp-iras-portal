using System.Text.Json;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
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

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
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

        Mocker.GetMock<IQuestionSetService>()
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

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
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
}