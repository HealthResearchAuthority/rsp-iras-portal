using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.RespondentServiceTests;

public class GetRespondentAnswers_Application : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task Returns_List(string applicationId)
    {
        // Arrange
        var answers = new List<RespondentAnswerDto>();
        Mocker.GetMock<IRespondentServiceClient>()
            .Setup(c => c.GetRespondentAnswers(applicationId))
            .ReturnsAsync(ApiResponseFactory.Success<IEnumerable<RespondentAnswerDto>>(answers));

        // Act
        var result = await Sut.GetRespondentAnswers(applicationId);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
    }
}