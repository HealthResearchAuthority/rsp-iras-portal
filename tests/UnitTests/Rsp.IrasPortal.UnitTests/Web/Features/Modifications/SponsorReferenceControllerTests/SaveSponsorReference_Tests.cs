using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.SponsorReferenceControllerTests;

public class SaveSponsorReference_Tests : TestServiceBase<SponsorReferenceController>
{
    private const string SectionId = "pm-sponsor-reference";
    private const string CategoryId = "Sponsor reference";

    [Fact]
    public async Task Returns_BadRequest_When_ModificationId_Missing()
    {
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var result = await Sut.SaveSponsorReference(new QuestionnaireViewModel());

        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Returns_ServiceError_When_QuestionSet_Fails()
    {
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        var modId = Guid.NewGuid();
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [Rsp.Portal.Application.Constants.TempDataKeys.ProjectModification.ProjectModificationId] = modId
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string?>(x => x == SectionId), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { StatusCode = System.Net.HttpStatusCode.BadRequest });

        var result = await Sut.SaveSponsorReference(new QuestionnaireViewModel());

        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Returns_View_When_Validation_Fails()
    {
        var http = new DefaultHttpContext();
        http.Items[Rsp.Portal.Application.Constants.ContextItemKeys.UserId] = "R1";
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [Rsp.Portal.Application.Constants.TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid()
        };

        // Question set returns one question
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string?>(x => x == SectionId), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections = [new SectionModel { Id = SectionId, CategoryId = CategoryId, Questions = [new QuestionModel { Id = "Q1", QuestionId = "Q1", Name = "Q1", AnswerDataType = "Text", CategoryId = CategoryId }] }]
                }
            });

        // Validator returns errors
        Mocker.GetMock<FluentValidation.IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<FluentValidation.ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult([new FluentValidation.Results.ValidationFailure("Q1", "err")]));

        var model = new QuestionnaireViewModel
        {
            Questions = [new QuestionViewModel { Index = 0, QuestionId = "Q1", DataType = "Text", Category = CategoryId, SectionId = SectionId, Answers = [] }]
        };

        var result = await Sut.SaveSponsorReference(model);

        var view = result.ShouldBeOfType<ViewResult>();
        var outModel = view.Model.ShouldBeOfType<QuestionnaireViewModel>();
        outModel.Questions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Redirects_To_ReviewAllChanges_On_Success()
    {
        var http = new DefaultHttpContext();
        http.Items[Rsp.Portal.Application.Constants.ContextItemKeys.UserId] = "R1";
        Sut.ControllerContext = new() { HttpContext = http };
        var modId = Guid.NewGuid();
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [Rsp.Portal.Application.Constants.TempDataKeys.ProjectModification.ProjectModificationId] = modId,
            [Rsp.Portal.Application.Constants.TempDataKeys.ProjectRecordId] = "PR1",
            [Rsp.Portal.Application.Constants.TempDataKeys.IrasId] = "IRAS",
            [Rsp.Portal.Application.Constants.TempDataKeys.ShortProjectTitle] = "Short"
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string?>(x => x == SectionId), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections = [new SectionModel { Id = SectionId, CategoryId = CategoryId, Questions = [new QuestionModel { Id = "Q1", QuestionId = "Q1", Name = "Q1", AnswerDataType = "Text", CategoryId = CategoryId }] }]
                }
            });

        // Validator OK both passes
        Mocker.GetMock<FluentValidation.IValidator<QuestionnaireViewModel>>()
            .SetupSequence(v => v.ValidateAsync(It.IsAny<FluentValidation.ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult())
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Save answers path
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = System.Net.HttpStatusCode.OK });

        var model = new QuestionnaireViewModel
        {
            Questions = [new QuestionViewModel { Index = 0, QuestionId = "Q1", DataType = "Text", Category = CategoryId, SectionId = SectionId, Answers = [] }]
        };

        var result = await Sut.SaveSponsorReference(model);

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:reviewallchanges");
        redirect.RouteValues!["projectRecordId"].ShouldBe("PR1");
        redirect.RouteValues!["irasId"].ShouldBe("IRAS");
        redirect.RouteValues!["shortTitle"].ShouldBe("Short");
        redirect.RouteValues!["projectModificationId"].ShouldBe(modId);
    }

    [Fact]
    public async Task Redirects_To_PostApproval_When_SaveForLater()
    {
        var http = new DefaultHttpContext();
        http.Items[Rsp.Portal.Application.Constants.ContextItemKeys.UserId] = "R1";
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [Rsp.Portal.Application.Constants.TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [Rsp.Portal.Application.Constants.TempDataKeys.ProjectRecordId] = "PR1",
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string?>(x => x == SectionId), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { Sections = [new() { Id = SectionId, CategoryId = CategoryId, Questions = [new QuestionModel { Id = "Q1", QuestionId = "Q1", Name = "Q1", AnswerDataType = "Text", CategoryId = CategoryId }] }] }
            });

        Mocker.GetMock<FluentValidation.IValidator<QuestionnaireViewModel>>()
            .SetupSequence(v => v.ValidateAsync(It.IsAny<FluentValidation.ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult())
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = System.Net.HttpStatusCode.OK });

        var model = new QuestionnaireViewModel
        {
            Questions = [new QuestionViewModel { Index = 0, QuestionId = "Q1", DataType = "Text", Category = CategoryId, SectionId = SectionId, Answers = [] }]
        };

        var result = await Sut.SaveSponsorReference(model, saveForLater: true);

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");
        redirect.RouteValues!["projectRecordId"].ShouldBe("PR1");
    }

    [Fact]
    public async Task Redirects_To_SwsModifications_When_SaveForLater_And_ReviseAndAuthorise()
    {
        var http = new DefaultHttpContext();
        http.Items[Rsp.Portal.Application.Constants.ContextItemKeys.UserId] = "R1";
        Sut.ControllerContext = new() { HttpContext = http };

        var modId = Guid.NewGuid();
        var sponsorUserId = Guid.NewGuid();
        var rtsId = "RTS-123";

        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = modId,
            [TempDataKeys.ProjectRecordId] = "PR1",
            [TempDataKeys.ProjectModification.ProjectModificationStatus] = ModificationStatus.ReviseAndAuthorise,
            [TempDataKeys.RevisionSponsorOrganisationUserId] = sponsorUserId,
            [TempDataKeys.RevisionRtsId] = rtsId
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string>(x => x == SectionId), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new()
                {
                    Sections =
                    [
                        new SectionModel
                    {
                        Id = SectionId,
                        CategoryId = CategoryId,
                        Questions = [ new QuestionModel { Id = "Q1", QuestionId = "Q1", AnswerDataType = "Text", CategoryId = CategoryId } ]
                    }
                    ]
                }
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .SetupSequence(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult())
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = System.Net.HttpStatusCode.OK });

        var model = new QuestionnaireViewModel
        {
            Questions =
            [
                new QuestionViewModel { Index = 0, QuestionId = "Q1", Category = CategoryId, SectionId = SectionId }
            ]
        };

        var result = await Sut.SaveSponsorReference(model, saveForLater: true);

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("sws:modifications");

        redirect.RouteValues!["sponsorOrganisationUserId"].ShouldBe(sponsorUserId);
        redirect.RouteValues!["rtsId"].ShouldBe(rtsId);
    }

    [Fact]
    public async Task Redirects_To_ModificationDetails_When_ReviseAndAuthorise_And_Not_SaveForLater()
    {
        var http = new DefaultHttpContext();
        http.Items[Rsp.Portal.Application.Constants.ContextItemKeys.UserId] = "R1";
        Sut.ControllerContext = new() { HttpContext = http };

        var modId = Guid.NewGuid();
        var sponsorUserId = Guid.NewGuid();
        var rtsId = "RTS-999";

        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = modId,
            [TempDataKeys.ProjectRecordId] = "PR1",
            [TempDataKeys.IrasId] = "IRAS",
            [TempDataKeys.ShortProjectTitle] = "ShortTitle",
            [TempDataKeys.ProjectModification.ProjectModificationStatus] = ModificationStatus.ReviseAndAuthorise,
            [TempDataKeys.RevisionSponsorOrganisationUserId] = sponsorUserId,
            [TempDataKeys.RevisionRtsId] = rtsId
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string>(x => x == SectionId), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new()
                {
                    Sections =
                    [
                        new SectionModel
                    {
                        Id = SectionId,
                        CategoryId = CategoryId,
                        Questions = [ new QuestionModel { Id = "Q1", QuestionId = "Q1", AnswerDataType = "Text", CategoryId = CategoryId } ]
                    }
                    ]
                }
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .SetupSequence(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult())
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = System.Net.HttpStatusCode.OK });

        var model = new QuestionnaireViewModel
        {
            Questions =
            [
                new QuestionViewModel { Index = 0, QuestionId = "Q1", Category = CategoryId, SectionId = SectionId }
            ]
        };

        var result = await Sut.SaveSponsorReference(model);

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:ModificationDetails");

        var rv = redirect.RouteValues!;
        rv["projectRecordId"].ShouldBe("PR1");
        rv["irasId"].ShouldBe("IRAS");
        rv["shortTitle"].ShouldBe("ShortTitle");
        rv["projectModificationId"].ShouldBe(modId);
        rv["sponsorOrganisationUserId"].ShouldBe(sponsorUserId);
        rv["rtsId"].ShouldBe(rtsId);
    }
}