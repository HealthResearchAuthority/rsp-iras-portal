using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.QuestionSetServiceTests;

public class CreateQuestionsTests : TestServiceBase<QuestionSetService>
{
    [Theory, AutoData]
    public async Task CreateQuestions_ShouldReturnServiceResponse_WithValidQuestions(QuestionSetDto questionSet)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.OK);

        Mocker
            .GetMock<IQuestionSetServiceClient>()
            .Setup(client => client.AddQuestionSet(questionSet))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.AddQuestionSet(questionSet);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task CreateQuestions_ShouldReturnError_WhenQuestionsAreNotValid(QuestionSetDto questionSet)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.BadRequest);

        Mocker
            .GetMock<IQuestionSetServiceClient>()
            .Setup(client => client.AddQuestionSet(questionSet))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.AddQuestionSet(questionSet);

        // Assert
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}