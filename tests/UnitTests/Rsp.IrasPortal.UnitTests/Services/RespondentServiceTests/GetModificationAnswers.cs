using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.RespondentServiceTests;

public class GetModificationAnswers : TestServiceBase<RespondentService>
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
            .Setup(c => c.GetModificationAnswers(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationAnswers(projectModificationChangeId, projectRecordId);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.GetModificationAnswers(projectModificationChangeId, projectRecordId),
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
            .Setup(c => c.GetModificationAnswers(projectModificationChangeId, categoryId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationAnswers(projectModificationChangeId, categoryId);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.GetModificationAnswers(projectModificationChangeId, categoryId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }
}