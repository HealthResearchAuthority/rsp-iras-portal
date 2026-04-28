using System.Security.Claims;
using System.Text.Json;
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
    public async Task RfiResponses_GET_Returns_View_With_Model(
    ModificationDetailsViewModel tempDataModel,
    Guid projectModificationId,
    ModificationRfiResponseResponse rfiResponseResponse)
    {
        // Arrange
        tempDataModel.ModificationId = projectModificationId.ToString();
        tempDataModel.Status = ModificationStatus.RequestForInformation;
        tempDataModel.SponsorOrganisationUserId = Guid.NewGuid().ToString();
        tempDataModel.RtsId = "12";
        // Fake RFI responses put into TempData (to be deserialized in GET)
        var rfiResponses = new List<RfiResponsesDTO>
        {
            new()
            {
                InitialResponse = new List<string> { "Test response" },
                ReasonForReviseAndAuthorise = new List<string> { "Test response" },
                RequestRevisionsByApplicant = new List<string>{ "Test response"},
                RequestRevisionsBySponsor= new List<string>{"Test response"},
                ReviseAndAuthorise = new List<string>{ "Test response" }
            },
            new()
            {
                InitialResponse = new List<string> { "Test response1" },
                ReasonForReviseAndAuthorise = new List<string> { "Test response1" },
                RequestRevisionsByApplicant = new List<string>{ "Test response1"},
                RequestRevisionsBySponsor= new List<string>{"Test response1"},
                ReviseAndAuthorise = new List<string>{ "Test response1" }
            }
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, Roles.Applicant) },
                "Test"))
            }
        };

        var tempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>()
        );

        tempData[TempDataKeys.IrasId] = "12345";
        tempData[TempDataKeys.ProjectRecordId] = "PR-001";
        tempData[TempDataKeys.ShortProjectTitle] = "Test project";
        tempData[TempDataKeys.ProjectModification.SponsorOrganisationUserId] = tempDataModel.SponsorOrganisationUserId;
        tempData[TempDataKeys.ProjectModification.RtsId] = tempDataModel.RtsId;
        tempData[TempDataKeys.RfiResponses] = JsonSerializer.Serialize(rfiResponses);
        tempDataModel.RfiModel.RfiResponses = rfiResponses;
        Sut.TempData = tempData;

        // Mock dependencies required by GET
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationResponse()
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(
                It.IsAny<string>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationReviewResponse()
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetDocumentsForModification(
                It.IsAny<Guid>(),
                It.IsAny<ProjectOverviewDocumentSearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<ProjectOverviewDocumentResponse>());

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationRfiResponses(
                It.IsAny<string>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ModificationRfiResponseResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = rfiResponseResponse
            });

        // changes
        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationChanges(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = [new() { Id = Guid.NewGuid(), AreaOfChange = "A1", SpecificAreaOfChange = "SA1", Status = ModificationStatus.InDraft }]
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
            .Setup(s => s.GetModificationChangeAnswers(It.IsAny<Guid>(), It.IsAny<string>()))
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
            .Setup(s => s.GetModificationAnswers(Guid.NewGuid(), It.IsAny<string>()))
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
        var result = await Sut.RfiResponses(
            projectRecordId: "PR-001",
            irasId: "12345",
            shortTitle: "Test project",
            projectModificationId: projectModificationId
        );

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<ModificationDetailsViewModel>();

        model.RfiModel.RfiResponses.ShouldNotBeEmpty();
        model.RfiModel.RfiResponses[0].InitialResponse[0]
            .ShouldBe("Test response");
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

    [Fact]
    public async Task SendRfiResponses_POST_RequestForInformation_Redirects_To_CheckAndSubmit()
    {
        // Arrange
        var model = new ModificationDetailsViewModel
        {
            ModificationId = Guid.NewGuid().ToString(),
            Status = ModificationStatus.RequestForInformation,
            SponsorOrganisationUserId = Guid.NewGuid().ToString(),
            RtsId = "12",
            RfiModel = new()
            {
                RfiReasons = ["Reason 1"],
                RfiResponses =
                [
                    new RfiResponsesDTO
                {
                    InitialResponse = ["Response 1"]
                }
                ]
            }
        };

        SetupTempData(model);
        SetupValidUser();
        SetupSuccessfulValidation();
        SetupSuccessfulSaveResponses();

        // Act
        var result = await Sut.SendRfiResponses(model, saveForLater: false);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.RfiCheckAndSubmitResponses));
    }

    [Fact]
    public async Task SendRfiResponses_POST_ResponseReviseAndAuthorise_Redirects_To_CheckAndSubmit()
    {
        // Arrange
        var model = new ModificationDetailsViewModel
        {
            ModificationId = Guid.NewGuid().ToString(),
            Status = ModificationStatus.ResponseReviseAndAuthorise,
            SponsorOrganisationUserId = Guid.NewGuid().ToString(),
            RtsId = "12",
            RfiModel = new()
            {
                RfiReasons = ["Reason 1"],
                RfiResponses =
                [
                    new RfiResponsesDTO
                {
                    ReviseAndAuthorise = ["Revised response"],
                    ReasonForReviseAndAuthorise = ["Reason for revision"]
                }
                ]
            }
        };

        SetupTempData(model);
        SetupValidUser();
        SetupSuccessfulValidation();
        SetupSuccessfulSaveResponses();

        // Act
        var result = await Sut.SendRfiResponses(model, saveForLater: false);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.RfiCheckAndSubmitResponses));
    }

    [Theory]
    [InlineData(ModificationStatus.Withdrawn)]
    public async Task SendRfiResponses_POST_Unsupported_Status_Returns_ServiceError(
    string status)
    {
        // Arrange
        var model = new ModificationDetailsViewModel
        {
            ModificationId = Guid.NewGuid().ToString(),
            Status = status,
            SponsorOrganisationUserId = Guid.NewGuid().ToString(),
            RtsId = "12",
            RfiModel = new()
            {
                RfiReasons = ["Reason 1"],
                RfiResponses =
                [
                    new RfiResponsesDTO
                {
                    InitialResponse = ["Response 1"]
                }
                ]
            }
        };

        SetupTempData(model);
        SetupValidUser();
        SetupSuccessfulValidation();

        // Act
        var result = await Sut.SendRfiResponses(model, saveForLater: false);

        // Assert
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
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
        model.SponsorOrganisationUserId = Guid.NewGuid().ToString();
        model.RtsId = "12";
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

    [Fact]
    public async Task RfiSubmitResponses_POST_RequestForInformation_Updates_Status_And_Redirects()
    {
        // Arrange
        var model = new ModificationDetailsViewModel
        {
            ModificationId = Guid.NewGuid().ToString(),
            ProjectRecordId = "PR-001",
            Status = ModificationStatus.RequestForInformation,
            SponsorOrganisationUserId = Guid.NewGuid().ToString(),
            RtsId = "12"
        };

        SetupTempData(model);
        SetupValidUser();

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(
                It.Is<UpdateModificationStatusRequest>(r =>
                    r.ProjectRecordId == model.ProjectRecordId &&
                    r.ModificationId == Guid.Parse(model.ModificationId!) &&
                    r.Status == ModificationStatus.ResponseWithSponsor
                )))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var result = await Sut.RfiSubmitResponses();

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.RfiResponsesConfirmation));
    }

    [Fact]
    public async Task RfiSubmitResponses_POST_ResponseReviseAndAuthorise_Updates_Status_And_Redirects()
    {
        // Arrange
        var model = new ModificationDetailsViewModel
        {
            ModificationId = Guid.NewGuid().ToString(),
            ProjectRecordId = "PR-002",
            Status = ModificationStatus.ResponseReviseAndAuthorise,
            SponsorOrganisationUserId = Guid.NewGuid().ToString(),
            RtsId = "12"
        };

        SetupTempData(model);
        SetupValidUser();

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(
                It.Is<UpdateModificationStatusRequest>(r =>
                    r.ProjectRecordId == model.ProjectRecordId &&
                    r.ModificationId == Guid.Parse(model.ModificationId!) &&
                    r.Status == ModificationStatus.ResponseWithReviewBody
                )))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var result = await Sut.RfiSubmitResponses();

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.RfiResponsesConfirmation));
    }

    [Theory]
    [InlineData(ModificationStatus.Withdrawn)]
    public async Task RfiSubmitResponses_POST_Unsupported_Status_Returns_ServiceError(
    string status)
    {
        // Arrange
        var model = new ModificationDetailsViewModel
        {
            ModificationId = Guid.NewGuid().ToString(),
            ProjectRecordId = "PR-003",
            Status = status,
            SponsorOrganisationUserId = Guid.NewGuid().ToString(),
            RtsId = "12"
        };

        SetupTempData(model);
        SetupValidUser();

        // Act
        var result = await Sut.RfiSubmitResponses();

        // Assert
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RfiSubmitResponses_POST_Service_Failure_Returns_ServiceError()
    {
        // Arrange
        var model = new ModificationDetailsViewModel
        {
            ModificationId = Guid.NewGuid().ToString(),
            ProjectRecordId = "PR-004",
            Status = ModificationStatus.RequestForInformation,
            SponsorOrganisationUserId = Guid.NewGuid().ToString(),
            RtsId = "12"
        };

        SetupTempData(model);
        SetupValidUser();

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(It.IsAny<UpdateModificationStatusRequest>()))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Error = "Boom"
            });

        // Act
        var result = await Sut.RfiSubmitResponses();

        // Assert
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
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

    [Theory, AutoData]
    public async Task SendRfiResponses_POST_Saves_And_Redirects_When_SaveForLater_Is_True_For_RequestRevision
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

        model.Status = ModificationStatus.ResponseWithSponsor;

        SetupTempData(model);
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var result = await Sut.SendRfiResponses(model, saveForLater: true);

        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("sws:modifications");
    }

    [Theory, AutoData]
    public async Task SendRfiResponses_POST_Saves_And_Redirects_When_SaveForLater_Is_False_For_RequestRevision
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

        model.Status = ModificationStatus.ResponseWithSponsor;

        SetupTempData(model);
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.BadRequest });

        var result = await Sut.SendRfiResponses(model, saveForLater: true);

        // Assert
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendRfiResponses_POST_RequestRevision_Redirects_To_CheckAndSubmit()
    {
        // Arrange
        var model = new ModificationDetailsViewModel
        {
            ModificationId = Guid.NewGuid().ToString(),
            Status = ModificationStatus.ResponseWithSponsor,
            SponsorOrganisationUserId = Guid.NewGuid().ToString(),
            RtsId = "12",
            RfiModel = new()
            {
                RfiReasons = ["Reason 1"],
                RfiResponses =
                [
                    new RfiResponsesDTO
                {
                    InitialResponse = ["Response 1"]
                }
                ]
            }
        };

        SetupTempData(model);
        SetupValidUser();
        SetupSuccessfulValidation();
        SetupSuccessfulSaveResponses();

        // Act
        var result = await Sut.SendRfiResponses(model, saveForLater: false);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.RfiCheckAndSubmitResponses));
    }

    [Fact]
    public async Task RfiSubmitResponses_POST_RequestRevision_Updates_Status_And_Redirects()
    {
        // Arrange
        var model = new ModificationDetailsViewModel
        {
            ModificationId = Guid.NewGuid().ToString(),
            ProjectRecordId = "PR-002",
            Status = ModificationStatus.ResponseWithSponsor,
            SponsorOrganisationUserId = Guid.NewGuid().ToString(),
            RtsId = "12"
        };

        SetupTempData(model);
        SetupValidUser();

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(
                It.Is<UpdateModificationStatusRequest>(r =>
                    r.ProjectRecordId == model.ProjectRecordId &&
                    r.ModificationId == Guid.Parse(model.ModificationId!) &&
                    r.Status == ModificationStatus.ResponseRequestRevisions
                )))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var result = await Sut.RfiSubmitResponses();

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.RfiResponsesConfirmation));
    }

    [Fact]
    public async Task RfiSubmitResponses_POST_RequestRevision_SaveForLater_And_Redirects()
    {
        // Arrange
        var model = new ModificationDetailsViewModel
        {
            ModificationId = Guid.NewGuid().ToString(),
            ProjectRecordId = "PR-002",
            Status = ModificationStatus.ResponseWithSponsor,
            SponsorOrganisationUserId = Guid.NewGuid().ToString(),
            RtsId = "12"
        };

        SetupTempData(model);
        SetupValidUser();

        // Act
        var result = await Sut.RfiSubmitResponses(true);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("sws:modifications");
    }

    private void SetupTempData(ModificationDetailsViewModel model)
    {
        if (model.IrasId == null)
        {
            model.IrasId = "123";
        }
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = model.ProjectRecordId,
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.Parse(model.ModificationId),
            [TempDataKeys.ShortProjectTitle] = model.ShortTitle,
            [TempDataKeys.IrasId] = model.IrasId.ToString(),
            [TempDataKeys.ProjectModification.SponsorOrganisationUserId] = model.SponsorOrganisationUserId,
            [TempDataKeys.ProjectModification.RtsId] = model.RtsId
        };
        if (model.Status is not null)
        {
            Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationStatus] = model.Status;
        }
    }

    private void SetupValidUser(params string[] roles)
    {
        var claims = roles.Any()
            ? roles.Select(r => new Claim(ClaimTypes.Role, r))
            : new[] { new Claim(ClaimTypes.Role, Roles.Applicant) };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, "Test"))
            }
        };
    }

    private void SetupSuccessfulSaveResponses()
    {
        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(
                It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });
    }

    private void SetupSuccessfulValidation()
    {
        Mocker.GetMock<IValidator<RfiResponsesDTO>>()
            .Setup(v => v.ValidateAsync(
                It.IsAny<IValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }
}