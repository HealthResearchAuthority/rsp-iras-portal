using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.ModificationChanges.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ModificationChanges;

public class ModificationChangesControllerTests : TestServiceBase<ModificationChangesController>
{
    private readonly Mock<IRespondentService> _respondentService;
    private readonly Mock<ICmsQuestionsetService> _cmsService;
    private readonly Mock<IValidator<QuestionnaireViewModel>> _validator;

    public ModificationChangesControllerTests()
    {
        _respondentService = Mocker.GetMock<IRespondentService>();
        _cmsService = Mocker.GetMock<ICmsQuestionsetService>();
        _validator = Mocker.GetMock<IValidator<QuestionnaireViewModel>>();
    }

    [Fact]
    public async Task DisplayQuestionnaire_Returns_Error_When_ModificationChangeId_Missing()
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.DisplayQuestionnaire("PR1", "CAT1", "SEC1", false, viewName: "PlannedEndDate");

        // Assert
        result
            .ShouldBeOfType<StatusCodeResult>()
            .StatusCode
            .ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task DisplayQuestionnaire_Returns_View_With_Model_When_Success()
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeId] = Guid.NewGuid()
        };

        _respondentService
            .Setup(s => s.GetModificationChangeAnswers(It.IsAny<Guid>(), "PR1", "CAT1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        // questions journey
        _cmsService
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate")
            });

        // stage resolution
        _cmsService
            .Setup(s => s.GetModificationQuestionSet("SEC1", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate")
            });

        _cmsService
            .Setup(s => s.GetModificationPreviousQuestionSection("SEC1", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "PREV", QuestionCategoryId = "CAT1", StaticViewName = "prev" }
            });

        _cmsService
            .Setup(s => s.GetModificationNextQuestionSection("SEC1", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "SEC2", QuestionCategoryId = "CAT1", StaticViewName = "AffectingOrganisations" }
            });

        // Act
        var result = await Sut.DisplayQuestionnaire("PR1", "CAT1", "SEC1", true, viewName: "PlannedEndDate");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("Questionnaire");
        var model = view.Model.ShouldBeOfType<QuestionnaireViewModel>();
        model.CurrentStage.ShouldBe("SEC1");
    }

    [Fact]
    public async Task SaveResponses_Returns_Error_View_When_GetQuestionSet_Fails()
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "PR1"
        };

        _cmsService
            .Setup(s => s.GetModificationQuestionSet("SEC1", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { StatusCode = HttpStatusCode.BadRequest, Error = "fail" });

        var model = new QuestionnaireViewModel { CurrentStage = "SEC1", Questions = [] };

        // Act
        var result = await Sut.SaveResponses(model);

        // Assert
        // Assert
        result
            .ShouldBeOfType<StatusCodeResult>()
            .StatusCode
            .ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task SaveResponses_Returns_View_With_Model_When_Validation_Fails()
    {
        // Arrange
        var (ctx, modChangeId) = SetupHttpContext();
        SetupStage("SEC1", "CAT1", currentStaticView: "PlannedEndDate", nextSectionId: "SEC2", nextStatic: "AffectingOrganisations");

        _cmsService
            .Setup(s => s.GetModificationQuestionSet("SEC1", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate",
                    new QuestionModel { Id = "QCMS1", QuestionId = "Q1", AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 })
            });

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Q1", "Required")]))
            .Verifiable();

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "SEC1",
            Questions = [new QuestionViewModel { Index = 0, QuestionId = "Q1", AnswerText = "" }]
        };

        // Act
        var result = await Sut.SaveResponses(model);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("Questionnaire");
        view.Model.ShouldBeOfType<QuestionnaireViewModel>();
        _validator.Verify();
    }

    [Fact]
    public async Task SaveResponses_Redirects_To_PostApproval_When_SaveForLater()
    {
        // Arrange
        var (ctx, modChangeId) = SetupHttpContext();
        SetupStage("SEC1", "CAT1", currentStaticView: "PlannedEndDate", nextSectionId: "SEC2", nextStatic: "AffectingOrganisations");

        _cmsService
            .Setup(s => s.GetModificationQuestionSet("SEC1", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate",
                    new QuestionModel { Id = "QCMS1", QuestionId = "Q1", AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 })
            });

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _respondentService
            .Setup(s => s.SaveModificationChangeAnswers(It.IsAny<ProjectModificationChangeAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "SEC1",
            Questions = [new QuestionViewModel { Index = 0, QuestionId = "Q1", AnswerText = "Some" }]
        };

        // Act
        var result = await Sut.SaveResponses(model, saveForLater: true);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");
        redirect.RouteValues!["projectRecordId"].ShouldBe("PR1");
    }

    [Fact]
    public async Task SaveResponses_Redirects_To_Review_When_CurrentSection_Mandatory_And_No_Answers()
    {
        // Arrange
        var (ctx, modChangeId) = SetupHttpContext();
        SetupStage("SEC1", "CAT1", currentStaticView: "PlannedEndDate", nextSectionId: "SEC2", nextStatic: "AffectingOrganisations", currentIsMandatory: true);

        _cmsService
            .Setup(s => s.GetModificationQuestionSet("SEC1", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate",
                    new QuestionModel { Id = "QCMS1", QuestionId = "Q1", AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 })
            });

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _respondentService
            .Setup(s => s.SaveModificationChangeAnswers(It.IsAny<ProjectModificationChangeAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "SEC1",
            Questions = [] // No posted answers -> treated as missing
        };

        // Act
        var result = await Sut.SaveResponses(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:AffectingOrganisations");
        redirect.RouteValues!["projectRecordId"].ShouldBe("PR1");
    }

    [Fact]
    public async Task SaveResponses_Redirects_To_Next_Section_When_Mandatory_With_Answers()
    {
        // Arrange
        var (ctx, modChangeId) = SetupHttpContext();
        SetupStage("SEC1", "CAT1", currentStaticView: "PlannedEndDate", nextSectionId: "SEC2", nextStatic: "AffectedOrganisationsType", currentIsMandatory: true);

        _cmsService
            .Setup(s => s.GetModificationQuestionSet("SEC1", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate",
                    new QuestionModel { Id = "QCMS1", QuestionId = "Q1", AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 })
            });

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _respondentService
            .Setup(s => s.SaveModificationChangeAnswers(It.IsAny<ProjectModificationChangeAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "SEC1",
            Questions = [new QuestionViewModel { Index = 0, QuestionId = "Q1", AnswerText = "Some" }]
        };

        // Act
        var result = await Sut.SaveResponses(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:AffectedOrganisationsType");
        redirect.RouteValues!["projectRecordId"].ShouldBe("PR1");
        redirect.RouteValues!["categoryId"].ShouldBe("CAT2");
        redirect.RouteValues!["sectionId"].ShouldBe("SEC2");
    }

    [Fact]
    public async Task SaveResponses_Redirects_To_Review_When_In_Review_Mode()
    {
        // Arrange
        var (ctx, modChangeId) = SetupHttpContext();
        // Set review mode flag
        Sut.TempData[TempDataKeys.ProjectModificationChange.ReviewChanges] = true;
        SetupStage("SEC1", "CAT1", currentStaticView: "PlannedEndDate", nextSectionId: "SEC2", nextStatic: "AffectingOrganisations");

        _cmsService
            .Setup(s => s.GetModificationQuestionSet("SEC1", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate",
                    new QuestionModel { Id = "QCMS1", QuestionId = "Q1", AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 })
            });

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _respondentService
            .Setup(s => s.SaveModificationChangeAnswers(It.IsAny<ProjectModificationChangeAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "SEC1",
            Questions = [new QuestionViewModel { Index = 0, QuestionId = "Q1", AnswerText = "Some" }]
        };

        // Act
        var result = await Sut.SaveResponses(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("ReviewChanges");
        redirect.RouteValues!["projectRecordId"].ShouldBe("PR1");
    }

    [Fact]
    public async Task DisplayQuestionnaire_Populates_OriginalAnswers_When_ShowOriginalAnswer_True()
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "PR1",
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeId] = Guid.NewGuid()
        };

        _cmsService
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate",
                    new QuestionModel { Id = "QCMS1", QuestionId = "Q1", ShowOriginalAnswer = true, AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 })
            });

        _respondentService
            .Setup(s => s.GetModificationChangeAnswers(It.IsAny<Guid>(), "PR1", "CAT1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        _respondentService
            .Setup(s => s.GetRespondentAnswers("PR1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                // The controller's transformation uses the CMS question Id as the QuestionViewModel.QuestionId,
                // so ensure the mocked original answer uses the CMS Id to match the resulting view model.
                Content = new[] { new RespondentAnswerDto { QuestionId = "QCMS1", AnswerText = "Original" } }
            });

        // Ensure SetStage has the required stage responses
        _cmsService
            .Setup(s => s.GetModificationQuestionSet("SEC1", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate",
                    new QuestionModel { Id = "QCMS1", QuestionId = "Q1", ShowOriginalAnswer = true, AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 })
            });

        _cmsService
            .Setup(s => s.GetModificationPreviousQuestionSection("SEC1", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "PREV", QuestionCategoryId = "CAT1", StaticViewName = "prev" }
            });

        _cmsService
            .Setup(s => s.GetModificationNextQuestionSection("SEC1", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "SEC2", QuestionCategoryId = "CAT1", StaticViewName = "AffectingOrganisations" }
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
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(questionSetServiceResponse);

        // Act
        var result = await Sut.DisplayQuestionnaire("PR1", "CAT1", "SEC1", false, "PlannedEndDate");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<QuestionnaireViewModel>();
        model.ProjectRecordAnswers.ShouldContainKey("QCMS1");
        model.ProjectRecordAnswers["QCMS1"].AnswerText.ShouldBe("Original");
    }

    private (DefaultHttpContext Ctx, Guid ModificationChangeId) SetupHttpContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items[ContextItemKeys.UserId] = "RESP1";
        Sut.ControllerContext = new ControllerContext { HttpContext = ctx };
        var modChangeId = Guid.NewGuid();
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = modChangeId,
            [TempDataKeys.ProjectRecordId] = "PR1",
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeId] = Guid.NewGuid()
        };
        return (ctx, modChangeId);
    }

    private void SetupStage(
        string currentSectionId,
        string currentCategoryId,
        string currentStaticView,
        string nextSectionId,
        string nextStatic,
        bool currentIsMandatory = false)
    {
        // For SetStage() usage
        _cmsService
            .Setup(s => s.GetModificationQuestionSet(currentSectionId, null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet(currentSectionId, currentCategoryId, currentStaticView, isMandatory: currentIsMandatory)
            });

        _cmsService
            .Setup(s => s.GetModificationPreviousQuestionSection(currentSectionId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "PREV", QuestionCategoryId = currentCategoryId, StaticViewName = "prev" }
            });

        _cmsService
            .Setup(s => s.GetModificationNextQuestionSection(currentSectionId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = nextSectionId, QuestionCategoryId = "CAT2", StaticViewName = nextStatic }
            });
    }

    private static CmsQuestionSetResponse BuildQuestionSet(
        string sectionId,
        string categoryId,
        string staticView,
        params QuestionModel[] questions) => BuildQuestionSet(sectionId, categoryId, staticView, false, questions);

    private static CmsQuestionSetResponse BuildQuestionSet(
        string sectionId,
        string categoryId,
        string staticView,
        bool isMandatory,
        params QuestionModel[] questions)
    {
        return new CmsQuestionSetResponse
        {
            Sections =
            [
                new SectionModel
                {
                    Id = sectionId,
                    SectionId = sectionId,
                    CategoryId = categoryId,
                    StaticViewName = staticView,
                    IsMandatory = isMandatory,
                    Questions = questions?.ToList() ?? [],
                    StoreUrlReferrer = true,
                }
            ]
        };
    }
}