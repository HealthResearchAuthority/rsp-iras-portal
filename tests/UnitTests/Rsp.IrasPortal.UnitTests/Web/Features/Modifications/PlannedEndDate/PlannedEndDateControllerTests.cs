using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.PlannedEndDate.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.PlannedEndDate;

public class PlannedEndDateControllerTests : TestServiceBase<PlannedEndDateController>
{
    private readonly Mock<IRespondentService> _respondentService;
    private readonly Mock<ICmsQuestionsetService> _cmsService;
    private readonly Mock<IValidator<QuestionnaireViewModel>> _validator;

    public PlannedEndDateControllerTests()
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
        var result = await Sut.DisplayQuestionnaire("PR1", "CAT1", "SEC1", false, viewName: nameof(PlannedEndDateController.PlannedEndDate));

        // Assert
        result.ShouldBeOfType<ViewResult>().ViewName.ShouldBe("Error");
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
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), "PR1", "CAT1"))
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
            .Setup(s => s.GetModificationPreviousQuestionSection("SEC1"))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "PREV", QuestionCategoryId = "CAT1", StaticViewName = "prev" }
            });

        _cmsService
            .Setup(s => s.GetModificationNextQuestionSection("SEC1"))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "SEC2", QuestionCategoryId = "CAT1", StaticViewName = "AffectingOrganisations" }
            });

        // Act
        var result = await Sut.DisplayQuestionnaire("PR1", "CAT1", "SEC1", true, viewName: nameof(PlannedEndDateController.PlannedEndDate));

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe(nameof(PlannedEndDateController.PlannedEndDate));
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
        result.ShouldBeOfType<ViewResult>().ViewName.ShouldBe("Error");
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
        view.ViewName.ShouldBe("PlannedEndDate");
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
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
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
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
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
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
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
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
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

    private (DefaultHttpContext Ctx, Guid ModificationChangeId) SetupHttpContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items[ContextItemKeys.RespondentId] = "RESP1";
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
            .Setup(s => s.GetModificationPreviousQuestionSection(currentSectionId))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "PREV", QuestionCategoryId = currentCategoryId, StaticViewName = "prev" }
            });

        _cmsService
            .Setup(s => s.GetModificationNextQuestionSection(currentSectionId))
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
                    Questions = questions?.ToList() ?? []
                }
            ]
        };
    }
}