using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.RespondentServiceTests;

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