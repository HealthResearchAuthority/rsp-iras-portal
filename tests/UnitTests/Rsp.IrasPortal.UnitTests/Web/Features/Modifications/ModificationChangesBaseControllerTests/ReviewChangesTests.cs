using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ModificationChangesBaseControllerTests;

public class ReviewChangesTests : TestServiceBase<ModificationChangesBaseController>
{
    private readonly Mock<IRespondentService> _respondentService;
    private readonly Mock<ICmsQuestionsetService> _cmsService;
    private readonly Mock<IValidator<QuestionnaireViewModel>> _validator;

    public ReviewChangesTests()
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

    [Theory, AutoData]
    public async Task ReviewChanges_Returns_Error_When_RespondentService_Fails(string projectRecordId, Guid specificAreaOfChangeId, Guid modificationChangeId)
    {
        // Arrange
        _respondentService
            .Setup(s => s.GetModificationChangeAnswers(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.BadRequest, Error = "fail" });

        // Act
        var result = await Sut.ReviewChanges(projectRecordId, specificAreaOfChangeId, modificationChangeId);

        // Assert
        // Assert
        result
            .ShouldBeOfType<StatusCodeResult>()
            .StatusCode
            .ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Theory, AutoData]
    public async Task ReviewChanges_Returns_Error_When_CmsJourney_Fails(string projectRecordId, Guid specificAreaOfChangeId, Guid modificationChangeId)
    {
        // Arrange
        _respondentService
            .Setup(s => s.GetModificationChangeAnswers(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        _cmsService
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { StatusCode = HttpStatusCode.BadRequest, Error = "fail" });

        // Act
        var result = await Sut.ReviewChanges(projectRecordId, specificAreaOfChangeId, modificationChangeId);

        // Assert
        // Assert
        result
            .ShouldBeOfType<StatusCodeResult>()
            .StatusCode
            .ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Theory, AutoData]
    public async Task ReviewChanges_Returns_View_With_Model_On_Success(string projectRecordId, Guid specificAreaOfChangeId, Guid modificationChangeId)
    {
        // Arrange
        _respondentService
            .Setup(s => s.GetModificationChangeAnswers(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        _cmsService
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet("SEC1", "CAT1", "PlannedEndDate",
                    new QuestionModel { Id = "QCMS1", QuestionId = "Q1", AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 })
            });

        _cmsService
            .Setup(s => s.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto
                {
                    AreasOfChange = [
                        new AreaOfChangeDto
                            {
                                SpecificAreasOfChange = [
                                    new AnswerModel
                                    {
                                        AutoGeneratedId = specificAreaOfChangeId.ToString(),
                                        OptionName = "Specific Area Name"
                                    }
                                ]
                            }
                    ]
                }
            });

        // Act
        var result = await Sut.ReviewChanges(projectRecordId, specificAreaOfChangeId, modificationChangeId);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("ModificationChangesReview");
        view.Model.ShouldBeOfType<QuestionnaireViewModel>();
    }

    [Fact]
    public async Task ReviewChanges_With_ReviseChange_Updates_TempData_And_Returns_View()
    {
        // Arranget
        var projectRecordId = "PR1";
        var specificAreaOfChangeId = Guid.NewGuid();
        var modificationChangeId = Guid.NewGuid();

        _respondentService
            .Setup(s => s.GetModificationChangeAnswers(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        var sectionId = "SEC1";
        var questionSetResponse = BuildQuestionSet(sectionId, "CAT1", "ReviewChanges",
            new QuestionModel { Id = "QCMS1", QuestionId = "Q1", AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 });

        _cmsService
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = questionSetResponse
            });

        _cmsService
            .Setup(s => s.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto
                {
                    AreasOfChange = [
                        new AreaOfChangeDto
                        {
                            SpecificAreasOfChange = [
                                new AnswerModel
                                {
                                    AutoGeneratedId = specificAreaOfChangeId.ToString(),
                                    OptionName = "Specific Area Name"
                                }
                            ]
                        }
                    ]
                }
            });

        _cmsService
            .Setup(s => s.GetModificationQuestionSet(sectionId, null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = questionSetResponse
            });

        _cmsService
            .Setup(s => s.GetModificationPreviousQuestionSection(sectionId))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "PREV", QuestionCategoryId = "CAT1" }
            });

        _cmsService
            .Setup(s => s.GetModificationNextQuestionSection(sectionId))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "NEXT", QuestionCategoryId = "CAT1" }
            });

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.ReviewChanges(projectRecordId, specificAreaOfChangeId, modificationChangeId, true);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("ModificationChangesReview");
        view.Model.ShouldBeOfType<QuestionnaireViewModel>();
        Sut.TempData[TempDataKeys.ProjectModification.SpecificAreaOfChangeText].ShouldBe("Specific Area Name");
        Sut.TempData[TempDataKeys.ProjectModification.SpecificAreaOfChangeId].ShouldBe(specificAreaOfChangeId);
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationChangeId].ShouldBe(modificationChangeId);
    }

    [Fact]
    public async Task ReviewChanges_With_ReviseChange_ValidationError_Returns_View()
    {
        // Arrange
        var projectRecordId = "PR1";
        var specificAreaOfChangeId = Guid.NewGuid();
        var modificationChangeId = Guid.NewGuid();

        _respondentService
            .Setup(s => s.GetModificationChangeAnswers(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        var sectionId = "SEC1";
        var questionSetResponse = BuildQuestionSet(sectionId, "CAT1", "ReviewChanges",
            new QuestionModel { Id = "QCMS1", QuestionId = "Q1", AnswerDataType = "Text", QuestionFormat = "text", CategoryId = "CAT1", Sequence = 1, SectionSequence = 1 });

        _cmsService
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = questionSetResponse
            });

        _cmsService
            .Setup(s => s.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto
                {
                    AreasOfChange = [
                        new AreaOfChangeDto
                        {
                            SpecificAreasOfChange = [
                                new AnswerModel
                                {
                                    AutoGeneratedId = specificAreaOfChangeId.ToString(),
                                    OptionName = "Specific Area Name"
                                }
                            ]
                        }
                    ]
                }
            });

        _cmsService
            .Setup(s => s.GetModificationQuestionSet(sectionId, null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = questionSetResponse
            });

        _cmsService
            .Setup(s => s.GetModificationPreviousQuestionSection(sectionId))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "PREV", QuestionCategoryId = "CAT1" }
            });

        _cmsService
            .Setup(s => s.GetModificationNextQuestionSection(sectionId))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { SectionId = "NEXT", QuestionCategoryId = "CAT1" }
            });

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Q1", "Required")]));

        // Act
        var result = await Sut.ReviewChanges(projectRecordId, specificAreaOfChangeId, modificationChangeId, true);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("ModificationChangesReview");
        view.Model.ShouldBeOfType<QuestionnaireViewModel>();
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState.ContainsKey("Q1").ShouldBeTrue();
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