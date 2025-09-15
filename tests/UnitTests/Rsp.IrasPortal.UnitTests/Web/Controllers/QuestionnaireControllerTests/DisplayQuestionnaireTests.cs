using System.Text.Json;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
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

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        var questionSetServiceResponse = new ServiceResponse<CmsQuestionSetResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new CmsQuestionSetResponse
            {
                Id = "123",
                Version = "1.0",
                Sections = new List<SectionModel>
                {
                    new SectionModel
                    {
                        SectionId = "section-1",
                        Questions = new List<QuestionModel>
                        {
                            new QuestionModel
                            {
                                QuestionId = "Q1"
                            }
                        }
                    }
                }
            }
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(questionSetServiceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

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
            .GetMock<ICmsQuestionsetService>()
            .Verify(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
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
        var response = new ServiceResponse<CmsQuestionSetResponse>
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = questionSectionsResponse
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
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
            List<QuestionSectionsResponse> questionSectionsResponse
        )
    {
        // Arrange
        var questionSetServiceResponse = new ServiceResponse<CmsQuestionSetResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new CmsQuestionSetResponse
            {
                Id = "123",
                Version = "1.0",
                Sections = new List<SectionModel>
                {
                    new SectionModel
                    {
                        SectionId = "section-1",
                        Questions = new List<QuestionModel>
                        {
                            new QuestionModel
                            {
                                QuestionId = "Q1"
                            }
                        }
                    }
                }
            }
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(questionSetServiceResponse);

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

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

        var questions = questionSetServiceResponse.Content.Sections.FirstOrDefault()?.Questions
            .Select((question, index) => (question, index));

        var expectedQuestionnaire = new QuestionnaireViewModel
        {
            CurrentStage = categoryId,
            Questions = questions.Select(q => new QuestionViewModel
            {
                Index = q.index,
                QuestionId = q.question.QuestionId,
                VersionId = q.question.Version ?? string.Empty,
                Category = q.question.CategoryId,
                Heading = q.question.Key,
                QuestionText = q.question.Label,
                QuestionType = q.question.QuestionFormat,
                Answers = q.question.Answers.Select(ans => new AnswerViewModel
                {
                    AnswerId = ans.Id,
                    AnswerText = ans.AutoGeneratedId
                }).ToList()
            }).ToList()
        };

        // Act
        var result = await Sut.DisplayQuestionnaire(categoryId, sectionId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");

        var model = viewResult.Model.ShouldBeOfType<QuestionnaireViewModel>();
        //model.Questions.ShouldBeEquivalentTo(expectedQuestionnaire.Questions);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Verify(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}