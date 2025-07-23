using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.RespondentServiceTests;

public class GetRespondentAnswers : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task GetRespondentAnswers_ByApplicationId_DelegatesToClient_AndReturnsMappedResult
    (
        string applicationId,
        List<RespondentAnswerDto> apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<IEnumerable<RespondentAnswerDto>>(httpResponseMessage, apiResponseContent, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetRespondentAnswers(applicationId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetRespondentAnswers(applicationId);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.GetRespondentAnswers(applicationId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }

    [Theory, AutoData]
    public async Task GetRespondentAnswers_ByApplicationIdAndCategory_DelegatesToClient_AndReturnsMappedResult
    (
        string applicationId,
        string categoryId,
        List<RespondentAnswerDto> apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<IEnumerable<RespondentAnswerDto>>(httpResponseMessage, apiResponseContent, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetRespondentAnswers(applicationId, categoryId);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.GetRespondentAnswers(applicationId, categoryId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }
}