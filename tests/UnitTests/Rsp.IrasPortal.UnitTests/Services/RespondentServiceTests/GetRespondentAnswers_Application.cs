using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.RespondentServiceTests;

public class GetRespondentAnswers_Application : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task Returns_List(string applicationId)
    {
        // Arrange
        var answers = new List<Application.DTOs.Requests.RespondentAnswerDto>();
        Mocker.GetMock<IRespondentServiceClient>()
            .Setup(c => c.GetRespondentAnswers(applicationId))
            .ReturnsAsync(ApiResponseFactory.Success<IEnumerable<Application.DTOs.Requests.RespondentAnswerDto>>(answers));

        // Act
        var result = await Sut.GetRespondentAnswers(applicationId);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
    }
}