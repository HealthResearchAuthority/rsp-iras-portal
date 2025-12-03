using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.SponsorReferenceControllerTests;

public class DisplayQuestionnaire_Tests : TestServiceBase<SponsorReferenceController>
{
    private const string SectionId = "pm-sponsor-reference";
    private const string CategoryId = "Sponsor reference";

    [Fact]
    public async Task Returns_BadRequest_When_ModificationId_Missing()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.DisplayQuestionnaire("PR1", CategoryId, SectionId);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Returns_View_On_Success()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [Rsp.IrasPortal.Application.Constants.TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid()
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), It.IsAny<string>(), CategoryId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = System.Net.HttpStatusCode.OK, Content = [] });

        var qset = new CmsQuestionSetResponse
        {
            Sections = [new SectionModel { Id = SectionId, CategoryId = CategoryId, Questions = [new QuestionModel { Id = "Q1", QuestionId = "Q1", Name = "Q1", CategoryId = CategoryId, AnswerDataType = "Text" }] }]
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string?>(x => x == SectionId), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { StatusCode = System.Net.HttpStatusCode.OK, Content = qset });

        // Act
        var result = await Sut.DisplayQuestionnaire("PR1", CategoryId, SectionId);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe(nameof(SponsorReferenceController.SponsorReference));
        var model = view.Model.ShouldBeOfType<QuestionnaireViewModel>();
        model.CurrentStage.ShouldBe(SectionId);
        model.Questions.Count.ShouldBe(1);
    }
}