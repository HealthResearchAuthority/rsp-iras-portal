using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.CmsQuestionsetServiceTests;

public class GetQuestionSetTests : TestServiceBase<CmsQuestionsetService>
{
    private readonly Mock<ICmsQuestionSetServiceClient> _questionSetServiceClient;

    public GetQuestionSetTests()
    {
        _questionSetServiceClient = Mocker.GetMock<ICmsQuestionSetServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetQuestionSet_ShouldReturnSuccess_WhenApiReturnsOk(
        string sectionId,
        string questionSetId)
    {
        // Arrange
        var apiResponse = new ApiResponse<CmsQuestionSetResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new CmsQuestionSetResponse(),
            new()
        );

        _questionSetServiceClient
            .Setup(c => c.GetQuestionSet(sectionId, questionSetId))
            .ReturnsAsync(apiResponse);

        var result = await Sut.GetQuestionSet(sectionId, questionSetId);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory, AutoData]
    public async Task GetQuestionSet_ShouldThrow_WhenApiFails(string sectionId, string questionSetId)
    {
        _questionSetServiceClient
            .Setup(c => c.GetQuestionSet(sectionId, questionSetId))
            .ThrowsAsync(new Exception("Internal server error"));

        await Should.ThrowAsync<Exception>(() => Sut.GetQuestionSet(sectionId, questionSetId));
    }
}