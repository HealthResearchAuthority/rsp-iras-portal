using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ApplicationsServiceTests;

public class GetApplicationsByRespondentTests : TestServiceBase<ApplicationsService>
{
    private readonly Mock<IApplicationsServiceClient> _applicationsServiceClient;

    public GetApplicationsByRespondentTests()
    {
        _applicationsServiceClient = Mocker.GetMock<IApplicationsServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetApplicationsByRespondent_Should_Return_Success_Response_When_Client_Returns_Success(string respondentId)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<IrasApplicationResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            [],
            new RefitSettings()
        );

        _applicationsServiceClient
            .Setup(c => c.GetApplicationsByRespondent(respondentId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetApplicationsByRespondent(respondentId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IEnumerable<IrasApplicationResponse>>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _applicationsServiceClient.Verify(c => c.GetApplicationsByRespondent(respondentId), Times.Once());
    }

    [Theory, AutoData]
    public async Task GetApplicationsByRespondent_Should_Return_Failure_Response_When_Client_Returns_Failure(string respondentId)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<IrasApplicationResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.NotFound),
            null,
            new RefitSettings()
        );

        _applicationsServiceClient
            .Setup(c => c.GetApplicationsByRespondent(respondentId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetApplicationsByRespondent(respondentId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IEnumerable<IrasApplicationResponse>>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        _applicationsServiceClient.Verify(c => c.GetApplicationsByRespondent(respondentId), Times.Once());
    }
}