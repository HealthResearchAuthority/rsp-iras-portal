using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.CmsQuestionsetServiceTests;

public class GetRelatedQuestionsTests : TestServiceBase<CmsQuestionsetService>
{
    [Fact]
    public async Task GetModificationRanking_ShouldReturnSuccess_WhenApiReturnsOk()
    {
        // Arrange
        var request = new RelatedQuestionsRequest { QuestionId = "area1", AnswerIds = ["applicability", "ProjectType"] };
        var apiResponse = new ApiResponse<List<string>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new(),
            new()
        );

        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(c => c.GetRelatedQuestions(It.IsAny<RelatedQuestionsRequest>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetRelatedQuestions(request);

        // Assert
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetModificationRanking_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        var request = new RelatedQuestionsRequest { QuestionId = "area1", AnswerIds = ["applicability", "ProjectType"] };
        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(c => c.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .ThrowsAsync(new Exception("API failure"));

        // Act & Assert
        await Should.ThrowAsync<Exception>(() => Sut.GetRelatedQuestions(request));
    }
}