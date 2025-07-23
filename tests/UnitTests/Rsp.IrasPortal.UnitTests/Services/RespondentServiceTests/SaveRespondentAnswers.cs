using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.RespondentServiceTests;

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
}