using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.CmsQuestionsetServiceTests;

public class GetModificationsJourneyTests : TestServiceBase<CmsQuestionsetService>
{
    private readonly Mock<ICmsQuestionSetServiceClient> _questionSetServiceClient;

    public GetModificationsJourneyTests()
    {
        _questionSetServiceClient = Mocker.GetMock<ICmsQuestionSetServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetModificationsJourney_ShouldReturnSuccess_WhenApiReturnsOk(
        string specificChangeId)
    {
        // Arrange
        var apiResponse = new ApiResponse<CmsQuestionSetResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new CmsQuestionSetResponse(),
            new()
        );

        _questionSetServiceClient
            .Setup(c => c.GetModificationsJourney(specificChangeId))
            .ReturnsAsync(apiResponse);

        var result = await Sut.GetModificationsJourney(specificChangeId);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory, AutoData]
    public async Task GetModificationsJourney_ShouldThrow_WhenApiFails(string specificChangeId)
    {
        _questionSetServiceClient
            .Setup(c => c.GetModificationsJourney(specificChangeId))
            .ThrowsAsync(new Exception("Bad request"));

        await Should.ThrowAsync<Exception>(() => Sut.GetModificationsJourney(specificChangeId));
    }
}