using System.Net;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ApplicationsServiceTests;

public class GetApplicationTests : TestServiceBase<ApplicationsService>
{
    private readonly Mock<IApplicationsServiceClient> _applicationsServiceClient;

    public GetApplicationTests()
    {
        _applicationsServiceClient = Mocker.GetMock<IApplicationsServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetApplication_Should_Return_Success_Response_When_Client_Returns_Success(string applicationId)
    {
        // Arrange
        var apiResponse = new ApiResponse<IrasApplicationResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new IrasApplicationResponse(),
            new()
        );

        _applicationsServiceClient
            .Setup(c => c.GetProjectRecord(applicationId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetProjectRecord(applicationId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IrasApplicationResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _applicationsServiceClient.Verify(c => c.GetProjectRecord(applicationId), Times.Once());
    }

    [Theory, AutoData]
    public async Task GetApplication_Should_Return_Failure_Response_When_Client_Returns_Failure(string applicationId)
    {
        // Arrange
        var apiResponse = new ApiResponse<IrasApplicationResponse>
        (
            new HttpResponseMessage(HttpStatusCode.NotFound),
            null,
            new()
        );

        _applicationsServiceClient
            .Setup(c => c.GetProjectRecord(applicationId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetProjectRecord(applicationId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IrasApplicationResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        _applicationsServiceClient.Verify(c => c.GetProjectRecord(applicationId), Times.Once());
    }
}