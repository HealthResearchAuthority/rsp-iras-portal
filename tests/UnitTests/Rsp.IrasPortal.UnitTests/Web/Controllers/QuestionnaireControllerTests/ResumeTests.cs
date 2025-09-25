using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
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
            .Setup(s => s.GetProjectRecord(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.NotFound });

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();

        // Verify
        Mocker
            .GetMock<IApplicationsService>()
            .Verify(s => s.GetProjectRecord(applicationId), Times.Once);
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
            .Setup(s => s.GetProjectRecord(applicationId))
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
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
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
            .GetMock<IApplicationsService>()
            .Setup(x => x.GetProjectRecord(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            { StatusCode = HttpStatusCode.OK, Content = new IrasApplicationResponse() });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(x => x.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentServiceResponse);

        var questionSetServiceResponse = new ServiceResponse<CmsQuestionSetResponse>
        {
            StatusCode = HttpStatusCode.InternalServerError,
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

        var questionsSetServiceSectionResponse = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new QuestionSectionsResponse
            {
                SectionName = "Test",
                QuestionCategoryId = categoryId
            }
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync(questionSetServiceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(x => x.GetQuestionSections(false))
            .ReturnsAsync(questionsSetServiceSectionsResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false))
            .ReturnsAsync(questionsSetServiceSectionResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(questionsSetServiceSectionResponse);

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
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
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
            .GetMock<ICmsQuestionsetService>()
            .Setup(x => x.GetQuestionSections(false))
            .ReturnsAsync(questionsSetServiceSectionsResponse);

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(x => x.GetProjectRecord(applicationId))
            .ReturnsAsync(applicationResponse);

        Mocker
            .GetMock<IRespondentService>()
            .Setup(x => x.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentAnswers);

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
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync(questionSetServiceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false))
            .ReturnsAsync(questionsSetServiceSectionResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(questionsSetServiceSectionResponse);

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
            .GetMock<ICmsQuestionsetService>()
            .Verify(x => x.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>(), false), Times.Once);
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
            .Setup(x => x.GetProjectRecord(applicationId))
            .ReturnsAsync(applicationResponse);

        Mocker
            .GetMock<IRespondentService>()
            .Setup(x => x.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentAnswers);

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
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync(questionSetServiceResponse);

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false))
            .ReturnsAsync(questionsSetServiceSectionResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(questionsSetServiceSectionResponse);

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
            .Setup(s => s.GetProjectRecord(applicationId))
            .ReturnsAsync(applicationResponse);

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentAnswers);

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
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync(questionSetServiceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(x => x.GetQuestionSections(false))
            .ReturnsAsync(questionsSetServiceSectionsResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false))
            .ReturnsAsync(questionsSetServiceSectionResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(questionsSetServiceSectionResponse);

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
            .Setup(s => s.GetProjectRecord(applicationId))
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
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSections(false))
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
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
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
            .Setup(s => s.GetProjectRecord(applicationId))
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
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSections(false))
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = sections
            });

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
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync(questionSetServiceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse()
            });

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false))
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
            .Setup(s => s.GetProjectRecord(applicationId))
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
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSections(false))
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

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
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync(questionSetServiceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse()
            });

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false))
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
    public async Task Resume_Uses_EmptySectionId_When_QuestionSections_NullOrEmpty
    (
        string applicationId,
        string categoryId
    )
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(applicationId))
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
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSections(false))
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

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
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync(questionSetServiceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse()
            });

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false))
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
            .Setup(s => s.GetProjectRecord(applicationId))
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
            new() { QuestionId = "1" }
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
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync(questionSetServiceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse()
            });

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false))
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