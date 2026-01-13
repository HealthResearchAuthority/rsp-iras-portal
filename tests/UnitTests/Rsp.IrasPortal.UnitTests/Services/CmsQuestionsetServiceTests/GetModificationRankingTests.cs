using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.CmsQuestionsetServiceTests;

public class GetModificationRankingTests : TestServiceBase<CmsQuestionsetService>
{
    [Fact]
    public async Task GetModificationRanking_ShouldReturnSuccess_WhenApiReturnsOk()
    {
        // Arrange
        var request = new RankingOfChangeRequest { SpecificAreaOfChangeId = "area1", Applicability = "applicability", ProjectType = "type", IsNHSInvolved = true, IsNonNHSInvolved = false };
        var apiResponse = new ApiResponse<RankingOfChangeResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new RankingOfChangeResponse(),
            new()
        );

        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(c => c.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationRanking(request);

        // Assert
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetModificationRanking_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        var request = new RankingOfChangeRequest { SpecificAreaOfChangeId = "area1", Applicability = "applicability", ProjectType = "type", IsNHSInvolved = true, IsNonNHSInvolved = false };
        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(c => c.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .ThrowsAsync(new Exception("API failure"));

        // Act & Assert
        await Should.ThrowAsync<Exception>(() => Sut.GetModificationRanking(request));
    }
}