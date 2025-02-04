using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ApplicationsServiceTests;

public class CreateApplicationTests : TestServiceBase<ApplicationsService>
{
    private readonly Mock<IApplicationsServiceClient> _applicationsServiceClient;

    public CreateApplicationTests()
    {
        _applicationsServiceClient = Mocker.GetMock<IApplicationsServiceClient>();
    }

    [Theory, AutoData]
    public async Task CreateApplication_Should_Return_Success_Response_When_Client_Returns_Success(IrasApplicationRequest irasApplication)
    {
        // Arrange
        var apiResponse = new ApiResponse<IrasApplicationResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new IrasApplicationResponse(),
            new()
        );

        _applicationsServiceClient
            .Setup(c => c.CreateApplication(irasApplication))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateApplication(irasApplication);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IrasApplicationResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _applicationsServiceClient.Verify(c => c.CreateApplication(irasApplication), Times.Once());
    }

    [Theory, AutoData]
    public async Task CreateApplication_Should_Return_Failure_Response_When_Client_Returns_Failure(IrasApplicationRequest irasApplication)
    {
        // Arrange
        var apiResponse = new ApiResponse<IrasApplicationResponse>
        (
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            null,
            new()
        );

        _applicationsServiceClient
            .Setup(c => c.CreateApplication(irasApplication))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateApplication(irasApplication);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IrasApplicationResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        _applicationsServiceClient.Verify(c => c.CreateApplication(irasApplication), Times.Once());
    }
}