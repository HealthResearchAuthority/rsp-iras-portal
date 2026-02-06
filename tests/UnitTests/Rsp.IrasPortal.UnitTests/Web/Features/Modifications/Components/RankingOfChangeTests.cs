using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Components;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.Components;

public class RankingOfChangeTests : TestServiceBase<RankingOfChange>
{
    public RankingOfChangeTests()
    {
        // Setup ViewComponentContext with TempData to avoid NullReferenceException
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        Sut.ViewComponentContext = new ViewComponentContext
        {
            ViewContext = new ViewContext
            {
                HttpContext = httpContext,
                TempData = tempData
            }
        };
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_View_With_RankingOfChangeViewModel_When_Substantial_Ranking_Response_Present()
    {
        // Arrange
        var questions = new List<QuestionViewModel>();
        var rankingResponse = new RankingOfChangeResponse
        {
            ModificationType = new ModificationRank { Substantiality = "Substantial", Order = 1 },
            Categorisation = new CategoryRank { Category = "CatA", Order = 2 },
            ReviewType = "TypeA"
        };
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<RankingOfChangeResponse> { Content = rankingResponse });

        // Act
        var result = await Sut.InvokeAsync(It.IsAny<string>(), "areaId", true, questions);

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        view.ViewName.ShouldBe("/Features/Modifications/Shared/RankingOfChange.cshtml");
        view.ViewData.ShouldNotBeNull();
        var model = view.ViewData.Model.ShouldBeOfType<RankingOfChangeViewModel>();
        model.ModificationType.ShouldBe("Substantial");
        model.Category.ShouldBe("CatA");
        model.ReviewType.ShouldBe("TypeA");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_View_With_RankingOfChangeViewModel_When_Ranking_Response_Present()
    {
        // Arrange
        var questions = new List<QuestionViewModel>();
        var rankingResponse = new RankingOfChangeResponse
        {
            ModificationType = new ModificationRank { Substantiality = "Non-Notifiable", Order = 1 },
            Categorisation = new CategoryRank { Category = "CatA", Order = 2 },
            ReviewType = "TypeA"
        };
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<RankingOfChangeResponse> { Content = rankingResponse });

        // Act
        var result = await Sut.InvokeAsync(It.IsAny<string>(), "areaId", true, questions);

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        view.ViewName.ShouldBe("/Features/Modifications/Shared/RankingOfChange.cshtml");
        view.ViewData.ShouldNotBeNull();
        var model = view.ViewData.Model.ShouldBeOfType<RankingOfChangeViewModel>();
        model.ModificationType.ShouldBe("Non-Notifiable");
        model.Category.ShouldBe("N/A");
        model.ReviewType.ShouldBe("TypeA");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_View_With_NotAvailable_When_Ranking_Response_Null()
    {
        // Arrange
        var answers = new List<RespondentAnswerDto>
            {
                new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
                new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
            };

        var questions = new List<QuestionViewModel>();
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<RankingOfChangeResponse> { Content = null });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), QuestionCategories.ProjectRecord))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        // Act
        var result = await Sut.InvokeAsync(It.IsAny<string>(), "areaId", false, questions);

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        view.ViewData.ShouldNotBeNull();
        var model = view.ViewData.Model.ShouldBeOfType<RankingOfChangeViewModel>();
        model.ModificationType.ShouldBe(Ranking.NotAvailable);
        model.Category.ShouldBe(Ranking.NotAvailable);
        model.ReviewType.ShouldBe(Ranking.NotAvailable);
    }

    [Fact]
    public async Task InvokeAsync_Should_Pass_Correct_Request_To_Service()
    {
        // Arrange
        var questions = new List<QuestionViewModel>
        {
            new() { NhsInvolvment = "NHS", Answers = [ new() { AnswerId = "A", AnswerText = "NHS", IsSelected = true } ] },
            new() { NonNhsInvolvment = "Non-NHS", Answers = [ new() { AnswerId = "B", AnswerText = "Non-NHS", IsSelected = true } ] }
        };
        RankingOfChangeRequest? capturedRequest = null;
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .Callback<RankingOfChangeRequest>(req => capturedRequest = req)
            .ReturnsAsync(new ServiceResponse<RankingOfChangeResponse> { Content = null });

        // Act
        await Sut.InvokeAsync(It.IsAny<string>(), "areaId", true, questions);

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.SpecificAreaOfChangeId.ShouldBe("areaId");
        capturedRequest.Applicability.ShouldBe("Yes");
        capturedRequest.IsNHSInvolved.ShouldBeTrue();
        capturedRequest.IsNonNHSInvolved.ShouldBeTrue();
    }
}