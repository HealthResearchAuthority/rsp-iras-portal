using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ApplicationsServiceTests;

public class GetApplicationsByStatusTests : TestServiceBase<ApplicationsService>
{
    private readonly Mock<IApplicationsServiceClient> _applicationsServiceClient;

    public GetApplicationsByStatusTests()
    {
        _applicationsServiceClient = Mocker.GetMock<IApplicationsServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetApplicationsByStatus_Should_Return_Success_Response_When_Client_Returns_Success(string status)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<IrasApplicationResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            [],
            new()
        );

        _applicationsServiceClient
            .Setup(c => c.GetApplicationsByStatus(status))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetApplicationsByStatus(status);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IEnumerable<IrasApplicationResponse>>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _applicationsServiceClient.Verify(c => c.GetApplicationsByStatus(status), Times.Once());
    }

    [Theory, AutoData]
    public async Task GetApplicationsByStatus_Should_Return_Failure_Response_When_Client_Returns_Failure(string status)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<IrasApplicationResponse>>
        (
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            null,
            new()
        );

        _applicationsServiceClient
            .Setup(c => c.GetApplicationsByStatus(status))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetApplicationsByStatus(status);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IEnumerable<IrasApplicationResponse>>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        // Verify
        _applicationsServiceClient.Verify(c => c.GetApplicationsByStatus(status), Times.Once());
    }
}