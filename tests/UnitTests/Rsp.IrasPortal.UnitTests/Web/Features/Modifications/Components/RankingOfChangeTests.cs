using Microsoft.AspNetCore.Mvc.ViewComponents;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.Components;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Components;

public class RankingOfChangeTests
{
    private readonly Mock<ICmsQuestionsetService> _cmsQuestionsetService;
    private readonly Mock<IRespondentService> _respondentService;
    private readonly RankingOfChange _sut;

    public RankingOfChangeTests()
    {
        _cmsQuestionsetService = new Mock<ICmsQuestionsetService>();
        _respondentService = new Mock<IRespondentService>();
        _sut = new RankingOfChange(_cmsQuestionsetService.Object, _respondentService.Object);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_View_With_RankingOfChangeViewModel_When_Ranking_Response_Present()
    {
        // Arrange
        var questions = new List<QuestionViewModel>();
        var rankingResponse = new RankingOfChangeResponse
        {
            ModificationType = new ModificationRank { Substantiality = "Substantial", Order = 1 },
            Categorisation = new CategoryRank { Category = "CatA", Order = 2 },
            ReviewType = "TypeA"
        };
        _cmsQuestionsetService
            .Setup(s => s.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<RankingOfChangeResponse> { Content = rankingResponse });

        // Act
        var result = await _sut.InvokeAsync(It.IsAny<string>(), "areaId", true, questions);

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        view.ViewName.ShouldBe("/Features/Modifications/Shared/RankingOfChange.cshtml");
        var model = view.ViewData.Model.ShouldBeOfType<RankingOfChangeViewModel>();
        model.ModificationType.ShouldBe("Substantial");
        model.Category.ShouldBe("CatA");
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
        _cmsQuestionsetService
            .Setup(s => s.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<RankingOfChangeResponse> { Content = null });

        _respondentService
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), QuestionCategories.ProjectRecrod))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        // Act
        var result = await _sut.InvokeAsync(It.IsAny<string>(), "areaId", false, questions);

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        var model = view.ViewData.Model.ShouldBeOfType<RankingOfChangeViewModel>();
        model.ModificationType.ShouldBe("Not available");
        model.Category.ShouldBe("N/A");
        model.ReviewType.ShouldBe("Not available");
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
        _cmsQuestionsetService
            .Setup(s => s.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .Callback<RankingOfChangeRequest>(req => capturedRequest = req)
            .ReturnsAsync(new ServiceResponse<RankingOfChangeResponse> { Content = null });

        // Act
        await _sut.InvokeAsync(It.IsAny<string>(), "areaId", true, questions);

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.SpecificAreaOfChangeId.ShouldBe("areaId");
        capturedRequest.Applicability.ShouldBe("Yes");
        capturedRequest.IsNHSInvolved.ShouldBeTrue();
        capturedRequest.IsNonNHSInvolved.ShouldBeTrue();
    }
}