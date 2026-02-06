using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ModificationRankingServiceTests;

public class CalculateChangeRankingTests : TestServiceBase<ModificationRankingService>
{
    [Fact]
    public async Task Returns_Default_Response_When_Journey_Fails()
    {
        // Arrange
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationsJourney("SA1"))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { StatusCode = System.Net.HttpStatusCode.BadRequest });

        // Act
        var result = await Sut.CalculateChangeRanking("PR1", "SA1", true, Guid.NewGuid());

        // Assert
        result.ShouldNotBeNull();
        result.ModificationType.ShouldNotBeNull();
        result.Categorisation.ShouldNotBeNull();
    }

    [Fact]
    public async Task Builds_Request_With_Forcing_Nhs_Involvement_When_Applicability_False_And_ProjectRecord_Yes()
    {
        // Arrange
        var changeId = Guid.NewGuid();
        var projectRecordAnswers = new List<RespondentAnswerDto> { new() { QuestionId = QuestionIds.NhsOrHscOrganisations, SelectedOption = QuestionAnswersOptionsIds.Yes } };
        var changeAnswers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = "Q1", Answers = ["A1"] },
            new() { QuestionId = "Q2", Answers = ["B1"] },
            new() { QuestionId = "Q3", SelectedOption = "C1" },
            new() { QuestionId = "Q4", SelectedOption = "D1" }
        };

        var questions = new List<QuestionModel>
        {
            new() { Id = "Q1", NhsInvolvment = "NHS", Answers = [ new() { Id = "A1", OptionName = "NHS" } ] },
            new() { Id = "Q2", NonNhsInvolvment = "Non-NHS", Answers = [ new() { Id = "B1", OptionName = "Non-NHS" } ] },
            new() { Id = "Q3", AffectedOrganisations = true, Answers = [ new() { Id = "C1", OptionName = "All" } ] },
            new() { Id = "Q4", RequireAdditionalResources = true, Answers = [ new() { Id = "D1", OptionName = "Yes" } ] }
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationsJourney("SA1"))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { StatusCode = HttpStatusCode.OK, Content = new CmsQuestionSetResponse { Sections = [new SectionModel { Questions = questions }] } });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers("PR1", QuestionCategories.ProjectRecord))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = projectRecordAnswers });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangeAnswers(changeId, "PR1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = changeAnswers });

        RankingOfChangeRequest? captured = null;
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .Callback<RankingOfChangeRequest>(r => captured = r)
            .ReturnsAsync(new ServiceResponse<RankingOfChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new RankingOfChangeResponse
                {
                    ModificationType = new() { Substantiality = Ranking.ModificationTypes.Substantial, Order = 1 },
                    Categorisation = new() { Category = Ranking.CategoryTypes.A, Order = 1 },
                    ReviewType = Ranking.ReviewTypes.ReviewRequired
                }
            });

        // Act
        var result = await Sut.CalculateChangeRanking("PR1", "SA1", false, changeId);

        // Assert
        captured.ShouldNotBeNull();
        captured.SpecificAreaOfChangeId.ShouldBe("SA1");
        captured.Applicability.ShouldBe("No");
        captured.IsNHSInvolved.ShouldBeTrue();
        captured.NhsOrganisationsAffected.ShouldBe("All");
        captured.IsNonNHSInvolved.ShouldBeTrue();
        captured.NhsResourceImplicaitons.ShouldBeTrue();
        result.ModificationType.Substantiality.ShouldBe(Ranking.ModificationTypes.Substantial);
    }
}