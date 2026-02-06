using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.CmsQuestionsetServiceTests;

public class GetQuestionSectionsTests : TestServiceBase<CmsQuestionsetService>
{
    private readonly Mock<ICmsQuestionSetServiceClient> _questionSetServiceClient;

    public GetQuestionSectionsTests()
    {
        _questionSetServiceClient = Mocker.GetMock<ICmsQuestionSetServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetQuestionSections_ShouldReturnSuccess_WhenApiReturnsOk(
        IEnumerable<QuestionSectionsResponse> expectedResponse)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<QuestionSectionsResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new List<QuestionSectionsResponse>(),
            new()
        );

        _questionSetServiceClient
            .Setup(c => c.GetQuestionSections())
            .ReturnsAsync(apiResponse);

        var result = await Sut.GetQuestionSections();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetQuestionSections_ShouldThrow_WhenApiFails()
    {
        _questionSetServiceClient
            .Setup(c => c.GetQuestionSections())
            .ThrowsAsync(new Exception("Timeout"));

        await Should.ThrowAsync<Exception>(() => Sut.GetQuestionSections());
    }
}