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
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications;

public class ModificationChangesBaseControllerTests : TestServiceBase<ModificationChangesBaseController>
{
    private readonly Mock<IRespondentService> _respondentService;
    private readonly Mock<ICmsQuestionsetService> _cmsService;
    private readonly Mock<IValidator<QuestionnaireViewModel>> _validator;

    public ModificationChangesBaseControllerTests()
    {
        _respondentService = Mocker.GetMock<IRespondentService>();
        _cmsService = Mocker.GetMock<ICmsQuestionsetService>();
        _validator = Mocker.GetMock<IValidator<QuestionnaireViewModel>>();

        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "PR1"
        };
    }

    [Fact]
    public async Task ReviewChanges_Returns_Error_When_RespondentService_Fails()
    {
        // Arrange
        _respondentService
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), "PR1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.BadRequest, Error = "fail" });

        // Act
        var result = await Sut.ReviewChanges("PR1");

        // Assert
        result.ShouldBeOfType<ViewResult>().ViewName.ShouldBe("Error");
    }

    [Fact]
    public async Task ReviewChanges_Returns_Error_When_CmsJourney_Fails()
    {
        // Arrange
        _respondentService
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), "PR1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        _cmsService
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { StatusCode = HttpStatusCode.BadRequest, Error = "fail" });

        // Act
        var result = await Sut.ReviewChanges("PR1");

        // Assert
        result.ShouldBeOfType<ViewResult>().ViewName.ShouldBe("Error");
    }

    [Fact]
    public async Task ReviewChanges_Returns_View_With_Model_On_Success()
    {
        // Arrange
        _respondentService
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), "PR1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        _cmsService
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate",
                    new QuestionModel { Id = "QCMS1", QuestionId = "Q1", AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 })
            });

        // Act
        var result = await Sut.ReviewChanges("PR1");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("ModificationChangesReview");
        view.Model.ShouldBeOfType<QuestionnaireViewModel>();
    }

    [Fact]
    public async Task ConfirmModificationChanges_Returns_Error_When_RespondentService_Fails()
    {
        // Arrange
        _respondentService
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), "PR1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.BadRequest, Error = "fail" });

        // Act
        var result = await Sut.ConfirmModificationChanges();

        // Assert
        result.ShouldBeOfType<ViewResult>().ViewName.ShouldBe("Error");
    }

    [Fact]
    public async Task ConfirmModificationChanges_Returns_Error_When_CmsJourney_Fails()
    {
        // Arrange
        _respondentService
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), "PR1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        _cmsService
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { StatusCode = HttpStatusCode.BadRequest, Error = "fail" });

        // Act
        var result = await Sut.ConfirmModificationChanges();

        // Assert
        result.ShouldBeOfType<ViewResult>().ViewName.ShouldBe("Error");
    }

    [Fact]
    public async Task ConfirmModificationChanges_Returns_View_When_Validation_Fails()
    {
        // Arrange
        _respondentService
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), "PR1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        _cmsService
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate",
                    new QuestionModel { Id = "QCMS1", QuestionId = "Q1", AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 })
            });

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([ new ValidationFailure("Q1", "Required") ]));

        // Act
        var result = await Sut.ConfirmModificationChanges();

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("ModificationChangesReview");
        view.Model.ShouldBeOfType<QuestionnaireViewModel>();
    }

    [Fact]
    public async Task ConfirmModificationChanges_Redirects_To_PostApproval_On_Success()
    {
        // Arrange
        _respondentService
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), "PR1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        _cmsService
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate",
                    new QuestionModel { Id = "QCMS1", QuestionId = "Q1", AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 })
            });

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.ConfirmModificationChanges();

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");
        redirect.RouteValues!["projectRecordId"].ShouldBe("PR1");
    }

    private static CmsQuestionSetResponse BuildQuestionSet(
        string sectionId,
        string categoryId,
        string staticView,
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
                    IsMandatory = false,
                    Questions = questions?.ToList() ?? []
                }
            ]
        };
    }
}
