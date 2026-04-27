using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Controllers;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.UnitTests;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.RfiResponses;

public class RfiResponseControllerTests : TestServiceBase<RfiResponseController>
{
    [Theory, AutoData]
    public async Task RfiDetails_Returns_View_When_No_Errors(
        string projectId,
        Guid modificationId,
        ProjectModificationResponse modificationResponse,
        IrasApplicationResponse projectRecordResponse,
        ProjectModificationReviewResponse rfiResponse,
        ModificationRfiResponseResponse rfiResponseResponse)
    {
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>());

        modificationResponse.Status = ModificationStatus.RequestForInformation;

        var modResponse = new ServiceResponse<ProjectModificationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification(projectId, modificationId))
            .ReturnsAsync(modResponse);

        var projectResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = projectRecordResponse
        };

        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(projectId))
            .ReturnsAsync(projectResponse);

        var modRfiResponse = new ServiceResponse<ProjectModificationReviewResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rfiResponse
        };

        var modRfiResponsesResponse = new ServiceResponse<ModificationRfiResponseResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rfiResponseResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(projectId, modificationId))
            .ReturnsAsync(modRfiResponse);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationRfiResponses(projectId, modificationId))
            .ReturnsAsync(modRfiResponsesResponse);

        var result = await Sut.RfiDetails(projectId, modificationId);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<ModificationDetailsViewModel>();
        model.IrasId.ShouldBe(projectRecordResponse.IrasId.ToString());
        model.ModificationId.ShouldBe(modificationId.ToString());
        model.RfiModel.RfiReasons.Count.ShouldBe(rfiResponse.RequestForInformationReasons.Count);
    }

    [Theory, AutoData]
    public async Task RfiDetails_Returns_View_And_Pads_RfiResponses_When_Fewer_Than_Reasons(
    string projectId,
    Guid modificationId,
    ProjectModificationResponse modificationResponse,
    IrasApplicationResponse projectRecordResponse,
    ProjectModificationReviewResponse rfiResponse,
    ModificationRfiResponseResponse rfiResponseResponse)
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>());

        modificationResponse.Status = ModificationStatus.RequestForInformation;

        rfiResponse.RequestForInformationReasons = new List<string>
        {
            "",
            "",
            ""
        };

        rfiResponseResponse.RfiResponses = new List<RfiResponsesDTO>
        {
            new() { InitialResponse = { "existing response" } }
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification(projectId, modificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = modificationResponse
            });

        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(projectId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = projectRecordResponse
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(projectId, modificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = rfiResponse
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationRfiResponses(projectId, modificationId))
            .ReturnsAsync(new ServiceResponse<ModificationRfiResponseResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = rfiResponseResponse
            });

        // Act
        var result = await Sut.RfiDetails(projectId, modificationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<ModificationDetailsViewModel>();

        model.RfiModel.RfiReasons.Count.ShouldBe(3);
        model.RfiModel.RfiResponses.Count.ShouldBe(3);

        model.RfiModel.RfiResponses
            .Skip(1)
            .All(r => r.InitialResponse.Single() == string.Empty)
            .ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task RfiDetails_Returns_Forbidden_When_Modification_Not_In_RFI(
       string projectId,
       Guid modificationId,
       ProjectModificationResponse modificationResponse,
       IrasApplicationResponse projectRecordResponse,
       ProjectModificationReviewResponse rfiResponse,
       ModificationRfiResponseResponse rfiResponsesResponse)
    {
        modificationResponse.Status = ModificationStatus.Received;

        var modResponse = new ServiceResponse<ProjectModificationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification(projectId, modificationId))
            .ReturnsAsync(modResponse);

        var projectResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = projectRecordResponse
        };

        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(projectId))
            .ReturnsAsync(projectResponse);

        var modRfiResponse = new ServiceResponse<ProjectModificationReviewResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rfiResponse
        };

        var modRfiResponsesResponse = new ServiceResponse<ModificationRfiResponseResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rfiResponsesResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(projectId, modificationId))
            .ReturnsAsync(modRfiResponse);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationRfiResponses(projectId, modificationId))
            .ReturnsAsync(modRfiResponsesResponse);

        var result = await Sut.RfiDetails(projectId, modificationId);

        var viewResult = result.ShouldBeOfType<ForbidResult>();
    }

    [Theory, AutoData]
    public async Task RfiResponses_GET_Returns_View_With_Model
    (
        ModificationDetailsViewModel tempDataModel,
        Guid modificationId
    )
    {
        tempDataModel.ModificationId = modificationId.ToString();
        tempDataModel.Status = ModificationStatus.RequestForInformation;
        SetupTempData(tempDataModel);
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Mocker.GetMock<IValidator<RfiResponsesDTO>>()
            .Setup(v => v.ValidateAsync(
                It.IsAny<IValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var result = await Sut.SendRfiResponses(tempDataModel, false);

        var view = result.ShouldBeOfType<RedirectToActionResult>();
    }

    [Theory, AutoData]
    public async Task SendRfiResponses_POST_Returns_View_With_Error_When_Reason_Missing
    (
        ModificationDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiModel.RfiReasons = ["Reason 1", "Reason 2"];
        model.RfiModel.RfiResponses = new List<RfiResponsesDTO>
        {
            new RfiResponsesDTO
            {
                InitialResponse = ["Response 1"]
            },
            new RfiResponsesDTO
            {
                InitialResponse = [""]
            }
        };

        model.Status = ModificationStatus.RequestForInformation;

        SetupTempData(model);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(
                It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        Mocker.GetMock<IValidator<RfiResponsesDTO>>()
            .Setup(v => v.ValidateAsync(
                It.IsAny<IValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors =
                {
                    new ValidationFailure(
                        propertyName: "InitialResponse[0]",
                        errorMessage: "Response is required")
                }
            });

        var result = await Sut.SendRfiResponses(model);

        var view = result.ShouldBeOfType<RedirectToActionResult>();
        view.ActionName.ShouldBe("RfiResponses");
        Sut.ModelState.IsValid.ShouldBeFalse();
    }

    [Theory, AutoData]
    public async Task SendRfiResponses_POST_Returns_Service_Error_When_Service_Fails
    (
        ModificationDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiModel.RfiReasons = ["Reason 1"];
        model.RfiModel.RfiResponses = new List<RfiResponsesDTO>
        {
            new RfiResponsesDTO
            {
                InitialResponse = ["Response 1"]
            },
        };

        model.Status = ModificationStatus.RequestForInformation;

        SetupTempData(model);
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.InternalServerError });

        Mocker.GetMock<IValidator<RfiResponsesDTO>>()
           .Setup(v => v.ValidateAsync(
               It.IsAny<IValidationContext>(),
               It.IsAny<CancellationToken>()))
           .ReturnsAsync(new ValidationResult());

        var result = await Sut.SendRfiResponses(model);
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(500);
    }

    [Theory, AutoData]
    public async Task SendRfiResponses_POST_Saves_And_Redirects_When_SaveForLater_Is_True
    (
        ModificationDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiModel.RfiReasons = ["Reason 1"];
        model.RfiModel.RfiResponses = new List<RfiResponsesDTO>
        {
            new RfiResponsesDTO
            {
                InitialResponse = ["Response 1"]
            },
        };

        model.Status = ModificationStatus.RequestForInformation;

        SetupTempData(model);
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var result = await Sut.SendRfiResponses(model, saveForLater: true);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("ReviewAllChanges");
    }

    [Theory, AutoData]
    public async Task SendRfiResponses_POST_Saves_And_Continues_When_SaveForLater_Is_False
    (
        ModificationDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiModel.RfiReasons = ["Reason 1"];
        model.RfiModel.RfiResponses = new List<RfiResponsesDTO>
        {
            new RfiResponsesDTO
            {
                InitialResponse = ["Response 1"]
            },
        };

        model.Status = ModificationStatus.RequestForInformation;

        SetupTempData(model);

        Mocker.GetMock<IValidator<RfiResponsesDTO>>()
           .Setup(v => v.ValidateAsync(
               It.IsAny<IValidationContext>(),
               It.IsAny<CancellationToken>()))
           .ReturnsAsync(new ValidationResult());

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var result = await Sut.SendRfiResponses(model, saveForLater: false);
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();

        redirectResult.ActionName.ShouldBe("RfiCheckAndSubmitResponses");
    }

    [Theory, AutoData]
    public async Task RfiCheckAndSubmitResponses_GET_Returns_View_With_Model
    (
        ModificationDetailsViewModel tempDataModel,
        Guid modificationId,
        ModificationRfiResponseResponse rfiResponseResponse,
        ProjectModificationReviewResponse rfiResponse
    )
    {
        tempDataModel.ModificationId = modificationId.ToString();
        SetupTempData(tempDataModel);
        var modRfiResponse = new ServiceResponse<ProjectModificationReviewResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rfiResponse
        };

        var modRfiResponsesResponse = new ServiceResponse<ModificationRfiResponseResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rfiResponseResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(tempDataModel.ProjectRecordId, modificationId))
            .ReturnsAsync(modRfiResponse);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationRfiResponses(tempDataModel.ProjectRecordId, modificationId))
            .ReturnsAsync(modRfiResponsesResponse);

        var result = await Sut.RfiCheckAndSubmitResponses();

        var view = result.ShouldBeOfType<ViewResult>();
        view.Model.ShouldBeAssignableTo<ModificationDetailsViewModel>();
    }

    [Theory, AutoData]
    public async Task RfiCheckAndSubmitResponses_POST_Returns_Service_Error_When_Service_Fails
    (
        ModificationDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiModel.RfiReasons = ["Reason 1"];
        model.RfiModel.RfiResponses = new List<RfiResponsesDTO> { new RfiResponsesDTO { InitialResponse = ["Response 1"] }, };
        model.Status = ModificationStatus.RequestForInformation;

        SetupTempData(model);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(
                It.Is<UpdateModificationStatusRequest>(r =>
                    r.ReasonNotApproved == null &&
                    r.Response == null &&
                    r.Role == null &&
                    r.ResponseOrigin == null
                )))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var result = await Sut.RfiSubmitResponses();
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(500);
    }

    [Theory, AutoData]
    public async Task RfiCheckAndSubmitResponses_POST_Submits_And_Redirects_When_Service_Succeeds
    (
        ModificationDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiModel.RfiReasons = ["Reason 1"];
        model.RfiModel.RfiResponses = new List<RfiResponsesDTO> { new RfiResponsesDTO { InitialResponse = ["Response 1"] }, };
        model.Status = ModificationStatus.RequestForInformation;

        SetupTempData(model);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(
                It.Is<UpdateModificationStatusRequest>(r =>
                    r.ReasonNotApproved == null &&
                    r.Response == null &&
                    r.Role == null &&
                    r.ResponseOrigin == null
                )))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        var result = await Sut.RfiSubmitResponses();
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();

        redirectResult.ActionName.ShouldBe("RfiResponsesConfirmation");
    }

    [Theory, AutoData]
    public async Task Returns_View_With_Changes_And_SponsorDetails
    (
        ModificationDetailsViewModel model,
        Guid modificationId
    )
    {
        // Arrange
        var modId = Guid.NewGuid();
        var changeId = Guid.NewGuid();

        model.ModificationId = modificationId.ToString();
        model.RfiModel.RfiReasons = ["Reason 1"];
        model.RfiModel.RfiResponses = new List<RfiResponsesDTO> { new RfiResponsesDTO { InitialResponse = ["Response 1"] }, };

        SetupTempData(model);
        // modification
        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification("PR1", modId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationResponse
                {
                    Id = modId,
                    ModificationIdentifier = modId.ToString(),
                    Status = ModificationStatus.InDraft,
                    ProjectRecordId = "PR1",
                    ModificationNumber = 1,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    CreatedBy = "TestUser",
                    UpdatedBy = "TestUser",
                    ModificationType = "Substantial",
                    Category = "Category A",
                    ReviewType = "Full Review"
                }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses("PR1", It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationReviewResponse
                {
                    ModificationId = modId,
                    RequestForInformationReasons = []
                }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationRfiResponses("PR1", modId))
            .ReturnsAsync(new ServiceResponse<ModificationRfiResponseResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ModificationRfiResponseResponse
                {
                    ModificationId = modId,
                    RfiResponses = []
                }
            });

        // changes
        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationChanges(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = [new() { Id = changeId, AreaOfChange = "A1", SpecificAreaOfChange = "SA1", Status = ModificationStatus.InDraft }]
            });

        // initial questions
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto
                {
                    AreasOfChange = [new() { AutoGeneratedId = "A1", OptionName = "Area Name", SpecificAreasOfChange = [new() { AutoGeneratedId = "SA1", OptionName = "Specific Name" }] }]
                }
            });

        // change journey and answers
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationsJourney("SA1"))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { Sections = [new() { Id = "S1", CategoryId = "C1", Questions = [new QuestionModel { Id = "Q1", QuestionId = "Q1", Name = "Q1", CategoryId = "C1", AnswerDataType = "Text" }] }] }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangeAnswers(changeId, It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationAuditTrail(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationAuditTrailResponse
                {
                    Items = [],
                    TotalCount = 0
                }
            });

        var answers = new List<RespondentAnswerDto>
            {
                new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
                new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
            };
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), QuestionCategories.ProjectRecord))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        // sponsor details question set and answers
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pm-sponsor-reference", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { Sections = [new() { Id = "S2", CategoryId = "SCAT", Questions = [new QuestionModel { Id = "SQ1", QuestionId = "SQ1", Name = "SQ1", CategoryId = "SCAT", AnswerDataType = "Text" }] }] }
            });

        var documents = new List<ProjectOverviewDocumentDto>
        {
            new() { FileName = "mod1", DocumentType = "TypeA" },
            new() { FileName = "mod2", DocumentType = "TypeB" }
        };

        var documentsResponse = new ProjectOverviewDocumentResponse
        {
            Documents = documents,
            TotalCount = documents.Count
        };

        var serviceResponse = new ServiceResponse<ProjectOverviewDocumentResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = documentsResponse
        };

        var projectModificationsService = Mocker.GetMock<IProjectModificationsService>();
        projectModificationsService
            .Setup(s => s.GetDocumentsForModification(It.IsAny<Guid>(), It.IsAny<ProjectOverviewDocumentSearchRequest>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // sponsor details question set and answers
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections = [new() { Id = "IQA0600", CategoryId = "SCAT", Questions = [new QuestionModel { Id = "IQA0600", QuestionId = "IQA0600", Name = "IQA0600", CategoryId = "SCAT", AnswerDataType = "Text",
                    Answers = new List<AnswerModel>
                        {
                            new AnswerModel { Id = "TypeB", OptionName = "actual text" }
                        } }] }]
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationAnswers(modId, It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        // Mock RankingOfChange response to avoid NullReferenceException
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<RankingOfChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new RankingOfChangeResponse
                {
                    ModificationType = new() { Substantiality = "Non-Notifiable", Order = 1 },
                    Categorisation = new() { Category = "Category", Order = 1 },
                    ReviewType = "ReviewType"
                }
            });

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.RfiResponses("PR1", "IRAS", "Short", modId, includeSelectiveDownloadError: true);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        model = view.Model.ShouldBeOfType<ModificationDetailsViewModel>();
        model.ModificationChanges.Count.ShouldBe(1);
    }

    private void SetupTempData(ModificationDetailsViewModel model)
    {
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = model.ProjectRecordId,
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.Parse(model.ModificationId),
            [TempDataKeys.ShortProjectTitle] = model.ShortTitle,
            [TempDataKeys.IrasId] = model.IrasId.ToString()
        };
        if (model.Status is not null)
        {
            Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationStatus] = model.Status;
        }
    }
}