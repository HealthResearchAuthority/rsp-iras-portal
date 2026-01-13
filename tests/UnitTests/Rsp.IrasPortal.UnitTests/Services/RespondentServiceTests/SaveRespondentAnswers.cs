using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.RespondentServiceTests;

public class SaveRespondentAnswers : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task SaveRespondentAnswers_DelegatesToClient_AndReturnsMappedResult
    (
        RespondentAnswersRequest request
    )
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.OK);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();

        respondentServiceClient
            .Setup(c => c.SaveRespondentAnswers(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveRespondentAnswers(request);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.SaveRespondentAnswers(request),
                Times.Once
            );

        result.ShouldNotBeNull();
    }

    [Theory, AutoData]
    public async Task SaveRespondentAnswers_Should_Return_Success_Response_When_Client_Returns_Success(RespondentAnswersRequest request)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.OK);

        Mocker
            .GetMock<IRespondentServiceClient>()
            .Setup(client => client.SaveRespondentAnswers(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveRespondentAnswers(request);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory, AutoData]
    public async Task SaveRespondentAnswers_Should_Return_Failure_Response_When_Client_Returns_Failure(RespondentAnswersRequest request)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.InternalServerError);

        Mocker
            .GetMock<IRespondentServiceClient>()
            .Setup(client => client.SaveRespondentAnswers(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveRespondentAnswers(request);

        // Assert
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Theory, AutoData]
    public async Task SaveRespondentAnswers_ShouldReturnServiceResponse_WithValidAnswers(RespondentAnswersRequest request)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.OK);

        Mocker
            .GetMock<IRespondentServiceClient>()
            .Setup(client => client.SaveRespondentAnswers(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveRespondentAnswers(request);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task SaveRespondentAnswers_ShouldReturnError_WhenAnswersAreNotValid(RespondentAnswersRequest request)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.BadRequest);

        Mocker
            .GetMock<IRespondentServiceClient>()
            .Setup(client => client.SaveRespondentAnswers(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveRespondentAnswers(request);

        // Assert
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}