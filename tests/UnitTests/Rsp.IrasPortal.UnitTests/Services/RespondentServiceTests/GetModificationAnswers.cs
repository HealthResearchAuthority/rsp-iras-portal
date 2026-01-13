using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.RespondentServiceTests;

public class GetModificationAnswers : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task GetModificationAnswers_ByModificationId_DelegatesToClient_AndReturnsMappedResult
    (
        Guid projectModificationId,
        string projectRecordId,
        List<RespondentAnswerDto> apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<IEnumerable<RespondentAnswerDto>>(httpResponseMessage, apiResponseContent, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetModificationAnswers(projectModificationId, projectRecordId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationAnswers(projectModificationId, projectRecordId);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.GetModificationAnswers(projectModificationId, projectRecordId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }

    [Theory, AutoData]
    public async Task GetModificationAnswers_ByModificationIdAndCategory_DelegatesToClient_AndReturnsMappedResult
    (
        Guid projectModificationId,
        string projectRecordId,
        string categoryId,
        List<RespondentAnswerDto> apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<IEnumerable<RespondentAnswerDto>>(httpResponseMessage, apiResponseContent, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetModificationAnswers(projectModificationId, projectRecordId, categoryId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationAnswers(projectModificationId, projectRecordId, categoryId);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.GetModificationAnswers(projectModificationId, projectRecordId, categoryId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }
}
