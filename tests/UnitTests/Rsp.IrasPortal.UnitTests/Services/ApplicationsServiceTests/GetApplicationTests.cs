using System.Net;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ApplicationsServiceTests;

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
            .Setup(c => c.GetApplication(applicationId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetApplication(applicationId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IrasApplicationResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _applicationsServiceClient.Verify(c => c.GetApplication(applicationId), Times.Once());
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
            .Setup(c => c.GetApplication(applicationId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetApplication(applicationId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IrasApplicationResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        _applicationsServiceClient.Verify(c => c.GetApplication(applicationId), Times.Once());
    }
}