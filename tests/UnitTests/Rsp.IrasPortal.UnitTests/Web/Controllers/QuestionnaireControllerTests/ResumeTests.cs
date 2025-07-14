using FluentValidation;
using FluentValidation.Results;
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

public class ResumeTests : TestServiceBase<QuestionnaireController>
{
    [Theory]
    [AutoData]
    public async Task Resume_ShouldReturnNotFound_WhenLoadApplicationReturnsNull(string applicationId,
        string categoryId)
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.NotFound });

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();

        // Verify
        Mocker
            .GetMock<IApplicationsService>()
            .Verify(s => s.GetApplication(applicationId), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Resume_Should_Return_ServiceError_When_RespondentService_GetRespondentAnswers_Is_Unsuccessful(
        string applicationId, string categoryId)
    {
        // Arrange
        var applicationResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new IrasApplicationResponse()
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(applicationResponse);

        var unsuccessfulResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(unsuccessfulResponse);

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult!.ViewName.ShouldBe("Error");
    }

    [Theory]
    [AutoData]
    public async Task Resume_Should_Return_ServiceError_When_QuestionSetService_GetQuestions_Is_Unsuccessful(
        string applicationId, string categoryId)
    {
        // Arrange
        var respondentServiceResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questionsSetServiceResponse = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        var questionsSetServiceSectionResponse = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content =
            [
                new()
                {
                    SectionName = "1",
                    QuestionCategoryId = "A"
                }
            ]
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(x => x.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            { StatusCode = HttpStatusCode.OK, Content = new IrasApplicationResponse() });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(x => x.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentServiceResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(x => x.GetQuestions(categoryId, It.IsAny<string>()))
            .ReturnsAsync(questionsSetServiceResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(x => x.GetQuestionSections())
            .ReturnsAsync(questionsSetServiceSectionResponse);

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
        viewResult.Model.ShouldBeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>();
    }

    [Theory]
    [AutoData]
    public async Task Resume_Should_UpdateQuestionnaireWithExistingAnswers_WhenRespondentAnswersNotEmpty(
        string applicationId, string categoryId, string sectionId)
    {
        // Arrange
        var applicationResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new IrasApplicationResponse()
        };

        var respondentAnswers = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questions = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questionsSetServiceSectionResponse = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new QuestionSectionsResponse
            {
                SectionName = "Test",
                QuestionCategoryId = categoryId,
                SectionId = sectionId
            }
        };

        var questionsSetServiceSectionsResponse = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content =
            [
                new()
                {
                    SectionName = "1",
                    QuestionCategoryId = "A"
                }
            ]
        };

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(x => x.GetQuestionSections())
            .ReturnsAsync(questionsSetServiceSectionsResponse);

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(x => x.GetApplication(applicationId))
            .ReturnsAsync(applicationResponse);

        Mocker
            .GetMock<IRespondentService>()
            .Setup(x => x.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentAnswers);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(x => x.GetQuestions(categoryId, It.IsAny<string>()))
            .ReturnsAsync(questions);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>()))
            .ReturnsAsync(questionsSetServiceSectionResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(questionsSetServiceSectionResponse);

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await Sut.Resume(applicationId, categoryId, sectionId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.DisplayQuestionnaire));
        redirectResult.RouteValues!["categoryId"].ShouldBe(categoryId);

        Mocker
            .GetMock<IRespondentService>()
            .Verify(x => x.GetRespondentAnswers(applicationId, categoryId), Times.Once);

        Mocker
            .GetMock<IQuestionSetService>()
            .Verify(x => x.GetQuestions(categoryId, It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Resume_Should_Return_View_With_Questionnaire_When_Validate_Is_True(string applicationId,
        string categoryId, string sectionId)
    {
        // Arrange
        var applicationResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new IrasApplicationResponse()
        };

        var respondentAnswers = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questions = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questionsSetServiceSectionResponse = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new QuestionSectionsResponse
            {
                SectionName = "Test",
                QuestionCategoryId = categoryId,
                SectionId = sectionId
            }
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(x => x.GetApplication(applicationId))
            .ReturnsAsync(applicationResponse);

        Mocker
            .GetMock<IRespondentService>()
            .Setup(x => x.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentAnswers);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(x => x.GetQuestions(categoryId, sectionId))
            .ReturnsAsync(questions);

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>()))
            .ReturnsAsync(questionsSetServiceSectionResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(questionsSetServiceSectionResponse);

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.Resume(applicationId, categoryId, "True", sectionId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");
        viewResult.Model.ShouldBeOfType<QuestionnaireViewModel>();

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Verify(
                x => x.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Resume_Should_RedirectToDisplayQuestionnaire_When_ValidateIsFalse(string applicationId,
        string categoryId, string sectionId)
    {
        // Arrange
        var applicationResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new IrasApplicationResponse()
        };

        var respondentAnswers = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questions = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questionsSetServiceSectionResponse = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new QuestionSectionsResponse
            {
                SectionName = "Test",
                QuestionCategoryId = categoryId,
                SectionId = sectionId
            }
        };

        var questionsSetServiceSectionsResponse = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content =
            [
                new()
                {
                    SectionName = "Test",
                    QuestionCategoryId = categoryId,
                    SectionId = sectionId
                }
            ]
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(applicationResponse);

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentAnswers);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions(categoryId, sectionId))
            .ReturnsAsync(questions);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(x => x.GetQuestionSections())
            .ReturnsAsync(questionsSetServiceSectionsResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>()))
            .ReturnsAsync(questionsSetServiceSectionResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(questionsSetServiceSectionResponse);

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.Resume(applicationId, categoryId, sectionId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.DisplayQuestionnaire));
        redirectResult.RouteValues!["categoryId"].ShouldBe(categoryId);
    }

    [Theory, AutoData]
    public async Task Resume_Should_Return_ServiceError_When_GetQuestionSections_Fails
    (
        string applicationId,
        string categoryId
    )
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse()
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestionSections())
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext { Session = session.Object };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
        viewResult.Model.ShouldBeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>();
    }

    [Theory, AutoData]
    public async Task Resume_Should_Return_ServiceError_When_RespondentService_GetModificationAnswers_Is_Unsuccessful
    (
        string applicationId,
        string categoryId,
        Guid modificationChangeId
    )
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse()
            });

        var unsuccessfulResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(unsuccessfulResponse);

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationAnswers(modificationChangeId, categoryId))
            .ReturnsAsync(unsuccessfulResponse);

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext { Session = session.Object };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
    }

    [Theory, AutoData]
    public async Task Resume_Should_Use_FirstSectionId_When_SectionId_Is_Null_And_Sections_Exist
    (
        string applicationId, string categoryId, string firstSectionId
    )
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse()
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        var sections = new List<QuestionSectionsResponse>
        {
            new() { SectionId = firstSectionId, QuestionCategoryId = categoryId, SectionName = "Section1" }
        };

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestionSections())
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = sections
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions(categoryId, firstSectionId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
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

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext { Session = session.Object };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.DisplayQuestionnaire));
        redirectResult.RouteValues!["categoryId"].ShouldBe(categoryId);
        redirectResult.RouteValues!["sectionId"].ShouldBe(firstSectionId);
    }

    [Theory, AutoData]
    public async Task Resume_Should_Handle_No_Sections_For_Category
    (
        string applicationId,
        string categoryId
    )
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse()
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        // No sections for the category
        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestionSections())
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions(categoryId, string.Empty))
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
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

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext { Session = session.Object };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.DisplayQuestionnaire));
        redirectResult.RouteValues!["categoryId"].ShouldBe(categoryId);
        redirectResult.RouteValues!["sectionId"].ShouldBe(null);
    }

    [Theory, AutoData]
    public async Task Resume_Calls_GetModificationAnswers_When_ModificationChangeId_NotEmpty
    (
        string applicationId,
        string categoryId,
        Guid modificationChangeId,
        string sectionId
    )
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse()
            });

        var respondentAnswers = new List<RespondentAnswerDto>();
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationAnswers(modificationChangeId, categoryId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = respondentAnswers
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions(categoryId, sectionId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
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

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext { Session = session.Object };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModificationId] = Guid.Empty,
            [TempDataKeys.ProjectModificationChangeId] = modificationChangeId,
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await Sut.Resume(applicationId, categoryId, "False", sectionId);

        // Assert
        Mocker
            .GetMock<IRespondentService>()
            .Verify(s => s.GetModificationAnswers(modificationChangeId, categoryId), Times.Once);
    }

    [Theory, AutoData]
    public async Task Resume_Uses_EmptySectionId_When_QuestionSections_NullOrEmpty
    (
        string applicationId,
        string categoryId
    )
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse()
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        // Simulate GetQuestionSections returns null content
        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestionSections())
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions(categoryId, string.Empty))
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
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

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext { Session = session.Object };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.RouteValues!["sectionId"].ShouldBe(null);
    }

    [Theory, AutoData]
    public async Task Resume_Filters_Questions_When_ModificationId_NotEmpty
    (
        string applicationId,
        string categoryId,
        string sectionId,
        Guid modificationId
    )
    {
        // Arrange
        var modificationChangeId = Guid.NewGuid();

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse()
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationAnswers(modificationChangeId, categoryId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
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

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext { Session = session.Object };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModificationId] = modificationId,
            [TempDataKeys.ProjectModificationChangeId] = modificationChangeId,
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await Sut.Resume(applicationId, categoryId, "False", sectionId);

        // Assert
        // Only the modification question should be present in the questionnaire
        // (We can't directly access the questionnaire, but we can check that the session was set with only one question)
        session
            .Verify(s => s.Set(It.Is<string>(k => k.Contains(sectionId)), It.IsAny<byte[]>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task Resume_Calls_UpdateWithAnswers_When_RespondentAnswers_NotEmpty
    (
        string applicationId,
        string categoryId,
        string sectionId
    )
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse()
            });

        var respondentAnswers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = "1", AnswerText = "Test" }
        };

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
            new() { QuestionId = "1", IsModificationQuestion = false }
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

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext { Session = session.Object };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await Sut.Resume(applicationId, categoryId, "False", sectionId);

        // Assert
        // The session should be set, indicating that UpdateWithAnswers was called and questionnaire was built
        session.Verify(s => s.Set(It.Is<string>(k => k.Contains(sectionId)), It.IsAny<byte[]>()), Times.Once);
    }
}