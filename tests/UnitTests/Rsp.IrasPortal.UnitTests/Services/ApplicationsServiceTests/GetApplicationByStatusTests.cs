using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ApplicationsServiceTests;

public class GetApplicationByStatusTests : TestServiceBase<ApplicationsService>
{
    private readonly Mock<IApplicationsServiceClient> _applicationsServiceClient;

    public GetApplicationByStatusTests()
    {
        _applicationsServiceClient = Mocker.GetMock<IApplicationsServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetApplicationByStatus_Should_Return_Success_Response_When_Client_Returns_Success(string applicationId, string status)
    {
        // Arrange
        var apiResponse = new ApiResponse<IrasApplicationResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new IrasApplicationResponse(),
            new()
        );

        _applicationsServiceClient
            .Setup(c => c.GetApplicationByStatus(applicationId, status))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetApplicationByStatus(applicationId, status);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IrasApplicationResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _applicationsServiceClient.Verify(c => c.GetApplicationByStatus(applicationId, status), Times.Once());
    }

    [Theory, AutoData]
    public async Task GetApplicationByStatus_Should_Return_Failure_Response_When_Client_Returns_Failure(string applicationId, string status)
    {
        // Arrange
        var apiResponse = new ApiResponse<IrasApplicationResponse>
        (
            new HttpResponseMessage(HttpStatusCode.NotFound),
            null,
            new()
        );

        _applicationsServiceClient
            .Setup(c => c.GetApplicationByStatus(applicationId, status))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetApplicationByStatus(applicationId, status);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IrasApplicationResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        _applicationsServiceClient.Verify(c => c.GetApplicationByStatus(applicationId, status), Times.Once());
    }
}