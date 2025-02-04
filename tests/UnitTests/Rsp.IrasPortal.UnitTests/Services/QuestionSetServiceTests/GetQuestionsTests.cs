using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.QuestionSetServiceTests;

public class GetQuestionsTests : TestServiceBase<QuestionSetService>
{
    private readonly Mock<IQuestionSetServiceClient> _questionSetServiceClient;

    public GetQuestionsTests()
    {
        _questionSetServiceClient = Mocker.GetMock<IQuestionSetServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetQuestions_Should_Return_Success_Response_When_Client_Returns_Success_With_Questions(List<QuestionsResponse> questionsResponse)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<QuestionsResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            questionsResponse,
            new()
        );

        _questionSetServiceClient
            .Setup(c => c.GetQuestions())
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetQuestions();

        // Assert
        result.ShouldNotBeNull();
        result.Content.ShouldBe(questionsResponse);
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetQuestions_Should_Return_Failure_Response_When_Client_Returns_Failure()
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<QuestionsResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            null,
            new()
        );

        _questionSetServiceClient
            .Setup(c => c.GetQuestions())
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetQuestions();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Theory, AutoData]
    public async Task GetQuestions_ByCategory_Should_Return_Success_Response_When_Client_Returns_Success_With_Questions(List<QuestionsResponse> questionsResponse, string categoryId)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<QuestionsResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            questionsResponse,
            new()
        );

        _questionSetServiceClient
            .Setup(c => c.GetQuestions(categoryId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetQuestions(categoryId);

        // Assert
        result.ShouldNotBeNull();
        result.Content.ShouldBe(questionsResponse);
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory, AutoData]
    public async Task GetQuestions_ByCategory_Should_Return_Failure_Response_When_Client_Returns_Failure(string categoryId)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<QuestionsResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            null,
            new()
        );

        _questionSetServiceClient
            .Setup(c => c.GetQuestions(categoryId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetQuestions(categoryId);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }
}