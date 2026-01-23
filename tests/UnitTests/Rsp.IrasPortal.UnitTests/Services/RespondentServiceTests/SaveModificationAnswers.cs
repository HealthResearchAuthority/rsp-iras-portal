using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.RespondentServiceTests;

public class SaveModificationAnswers : TestServiceBase<RespondentService>
{
    [Fact]
    public async Task Returns_Success_On_200()
    {
        // Arrange
        var req = new ProjectModificationAnswersRequest();

        var apiResponse = ApiResponseFactory.Success();

        Mocker
            .GetMock<IRespondentServiceClient>()
            .Setup(c => c.SaveModificationAnswers(req))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveModificationAnswers(req);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}