using Rsp.Portal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.CmsQuestionsetServiceTests;

public class GetInitialModificationQuestionsTests : TestServiceBase<CmsQuestionsetService>
{
    [Fact]
    public async Task GetInitialModificationQuestions_ShouldReturnSuccess_WhenApiReturnsOk()
    {
        // Arrange
        var apiResponse = new ApiResponse<StartingQuestionsDto>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new StartingQuestionsDto(),
            new()
        );

        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(c => c.GetInitialModificationQuestions())
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetInitialModificationQuestions();

        // Assert
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetInitialModificationQuestions_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(c => c.GetInitialModificationQuestions())
            .ThrowsAsync(new Exception("API failure"));

        // Act & Assert
        await Should.ThrowAsync<Exception>(() => Sut.GetInitialModificationQuestions());
    }
}