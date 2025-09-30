using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.RespondentServiceTests;

public class SaveModificationChangeAnswers : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task SaveModificationChangeAnswers_DelegatesToClient_AndReturnsMappedResult
    (
        ProjectModificationChangeAnswersRequest request
    )
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.OK);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();

        respondentServiceClient
            .Setup(c => c.SaveModificationChangeAnswers(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveModificationChangeAnswers(request);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.SaveModificationChangeAnswers(request),
                Times.Once
            );

        result.ShouldNotBeNull();
    }
}