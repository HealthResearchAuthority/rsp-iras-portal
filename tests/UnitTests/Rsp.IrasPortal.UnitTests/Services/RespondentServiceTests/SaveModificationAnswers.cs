using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.RespondentServiceTests;

public class SaveModificationAnswers : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task SaveModificationAnswers_DelegatesToClient_AndReturnsMappedResult
    (
        ProjectModificationAnswersRequest request
    )
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.OK);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();

        respondentServiceClient
            .Setup(c => c.SaveModificationAnswers(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveModificationAnswers(request);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.SaveModificationAnswers(request),
                Times.Once
            );

        result.ShouldNotBeNull();
    }
}