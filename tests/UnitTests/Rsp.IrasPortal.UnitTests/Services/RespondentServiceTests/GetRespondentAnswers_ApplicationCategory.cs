using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.RespondentServiceTests;

public class GetRespondentAnswers_ApplicationCategory : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task Returns_List(string applicationId, string category)
    {
        // Arrange
        var answers = new List<RespondentAnswerDto>();
        Mocker.GetMock<IRespondentServiceClient>()
            .Setup(c => c.GetRespondentAnswers(applicationId, category))
            .ReturnsAsync(ApiResponseFactory.Success<IEnumerable<RespondentAnswerDto>>(answers));

        // Act
        var result = await Sut.GetRespondentAnswers(applicationId, category);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
    }
}