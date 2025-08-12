using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.CmsQuestionsetServiceTests;

public class GetInitialModificationQuestionsTests : TestServiceBase<CmsQuestionsetService>
{
    [Fact]
    public async Task GetInitialModificationQuestions_ShouldReturnSuccess_WhenApiReturnsOk()
    {
        // Arrange
        var apiResponse = new ApiResponse<StartingQuestionsModel>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new StartingQuestionsModel(),
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