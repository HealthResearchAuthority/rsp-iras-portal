using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ApplicationsServiceTests;

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