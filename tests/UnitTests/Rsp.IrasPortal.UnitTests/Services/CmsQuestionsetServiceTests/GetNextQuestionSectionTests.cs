using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.CmsQuestionsetServiceTests;

public class GetNextQuestionSectionTests : TestServiceBase<CmsQuestionsetService>
{
    private readonly Mock<ICmsQuestionSetServiceClient> _questionSetServiceClient;

    public GetNextQuestionSectionTests()
    {
        _questionSetServiceClient = Mocker.GetMock<ICmsQuestionSetServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetNextQuestionSection_ShouldReturnSuccess_WhenApiReturnsOk(
        string currentSectionId)
    {
        // Arrange
        var apiResponse = new ApiResponse<QuestionSectionsResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new QuestionSectionsResponse(),
            new()
        );

        _questionSetServiceClient
            .Setup(c => c.GetNextQuestionSection(currentSectionId, false))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetNextQuestionSection(currentSectionId, false);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory, AutoData]
    public async Task GetNextQuestionSection_ShouldThrow_WhenApiFails(string currentSectionId)
    {
        _questionSetServiceClient
            .Setup(c => c.GetNextQuestionSection(currentSectionId, false))
            .ThrowsAsync(new Exception("API Down"));

        await Should.ThrowAsync<Exception>(() => Sut.GetNextQuestionSection(currentSectionId, false));
    }
}