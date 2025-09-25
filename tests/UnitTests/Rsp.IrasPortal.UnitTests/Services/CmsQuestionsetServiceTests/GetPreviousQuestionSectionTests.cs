using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.CmsQuestionsetServiceTests;

public class GetPreviousQuestionSectionTests : TestServiceBase<CmsQuestionsetService>
{
    private readonly Mock<ICmsQuestionSetServiceClient> _questionSetServiceClient;

    public GetPreviousQuestionSectionTests()
    {
        _questionSetServiceClient = Mocker.GetMock<ICmsQuestionSetServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetPreviousQuestionSection_ShouldReturnSuccess_WhenApiReturnsOk(
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
            .Setup(c => c.GetPreviousQuestionSection(currentSectionId, false))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetPreviousQuestionSection(currentSectionId, false);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory, AutoData]
    public async Task GetPreviousQuestionSection_ShouldThrow_WhenApiFails(string currentSectionId)
    {
        _questionSetServiceClient
            .Setup(c => c.GetPreviousQuestionSection(currentSectionId, false))
            .ThrowsAsync(new Exception("Service unavailable"));

        await Should.ThrowAsync<Exception>(() => Sut.GetPreviousQuestionSection(currentSectionId, false));
    }
}