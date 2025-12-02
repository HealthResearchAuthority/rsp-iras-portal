using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.SponsorReferenceControllerTests;

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
            [Rsp.IrasPortal.Application.Constants.TempDataKeys.ProjectModification.ProjectModificationId] = modId
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
        http.Items[Rsp.IrasPortal.Application.Constants.ContextItemKeys.UserId] = "R1";
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [Rsp.IrasPortal.Application.Constants.TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid()
        };

        // Question set returns one question
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string?>(x => x == SectionId), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections = [ new SectionModel { Id = SectionId, CategoryId = CategoryId, Questions = [ new QuestionModel { Id = "Q1", QuestionId = "Q1", Name = "Q1", AnswerDataType = "Text", CategoryId = CategoryId } ] } ]
                }
            });

        // Validator returns errors
        Mocker.GetMock<FluentValidation.IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<FluentValidation.ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult([ new FluentValidation.Results.ValidationFailure("Q1", "err") ]));

        var model = new QuestionnaireViewModel
        {
            Questions = [ new QuestionViewModel { Index = 0, QuestionId = "Q1", DataType = "Text", Category = CategoryId, SectionId = SectionId, Answers = [] } ]
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
        http.Items[Rsp.IrasPortal.Application.Constants.ContextItemKeys.UserId] = "R1";
        Sut.ControllerContext = new() { HttpContext = http };
        var modId = Guid.NewGuid();
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [Rsp.IrasPortal.Application.Constants.TempDataKeys.ProjectModification.ProjectModificationId] = modId,
            [Rsp.IrasPortal.Application.Constants.TempDataKeys.ProjectRecordId] = "PR1",
            [Rsp.IrasPortal.Application.Constants.TempDataKeys.IrasId] = "IRAS",
            [Rsp.IrasPortal.Application.Constants.TempDataKeys.ShortProjectTitle] = "Short"
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string?>(x => x == SectionId), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections = [ new SectionModel { Id = SectionId, CategoryId = CategoryId, Questions = [ new QuestionModel { Id = "Q1", QuestionId = "Q1", Name = "Q1", AnswerDataType = "Text", CategoryId = CategoryId } ] } ]
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
            Questions = [ new QuestionViewModel { Index = 0, QuestionId = "Q1", DataType = "Text", Category = CategoryId, SectionId = SectionId, Answers = [] } ]
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
        http.Items[Rsp.IrasPortal.Application.Constants.ContextItemKeys.UserId] = "R1";
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [Rsp.IrasPortal.Application.Constants.TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [Rsp.IrasPortal.Application.Constants.TempDataKeys.ProjectRecordId] = "PR1",
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string?>(x => x == SectionId), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { Sections = [ new() { Id = SectionId, CategoryId = CategoryId, Questions = [ new QuestionModel { Id = "Q1", QuestionId = "Q1", Name = "Q1", AnswerDataType = "Text", CategoryId = CategoryId } ] } ] }
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
            Questions = [ new QuestionViewModel { Index = 0, QuestionId = "Q1", DataType = "Text", Category = CategoryId, SectionId = SectionId, Answers = [] } ]
        };

        var result = await Sut.SaveSponsorReference(model, saveForLater: true);

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");
        redirect.RouteValues!["projectRecordId"].ShouldBe("PR1");
    }
}