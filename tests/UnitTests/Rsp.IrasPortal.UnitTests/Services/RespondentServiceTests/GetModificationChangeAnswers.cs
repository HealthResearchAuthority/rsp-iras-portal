using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.RespondentServiceTests;

public class GetModificationChangeAnswers : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task GetModificationAnswers_ByChangeId_DelegatesToClient_AndReturnsMappedResult
    (
        Guid projectModificationChangeId,
        string projectRecordId,
        List<RespondentAnswerDto> apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<IEnumerable<RespondentAnswerDto>>(httpResponseMessage, apiResponseContent, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetModificationChangeAnswers(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationChangeAnswers(projectModificationChangeId, projectRecordId);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.GetModificationChangeAnswers(projectModificationChangeId, projectRecordId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }

    [Theory, AutoData]
    public async Task GetModificationAnswers_ByChangeIdAndCategory_DelegatesToClient_AndReturnsMappedResult
    (
        Guid projectModificationChangeId,
        string categoryId,
        List<RespondentAnswerDto> apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var apiResponse = new ApiResponse<IEnumerable<RespondentAnswerDto>>(httpResponseMessage, apiResponseContent, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetModificationChangeAnswers(projectModificationChangeId, categoryId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationChangeAnswers(projectModificationChangeId, categoryId);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.GetModificationChangeAnswers(projectModificationChangeId, categoryId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }
}