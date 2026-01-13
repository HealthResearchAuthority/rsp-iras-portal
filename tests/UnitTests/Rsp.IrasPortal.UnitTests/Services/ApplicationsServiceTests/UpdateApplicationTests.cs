using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ApplicationsServiceTests;

public class UpdateApplicationTests : TestServiceBase<ApplicationsService>
{
    private readonly Mock<IApplicationsServiceClient> _applicationsServiceClient;

    public UpdateApplicationTests()
    {
        _applicationsServiceClient = Mocker.GetMock<IApplicationsServiceClient>();
    }

    [Theory, AutoData]
    public async Task UpdateApplication_Should_Return_Success_Response_When_Client_Returns_Success(IrasApplicationRequest irasApplication)
    {
        // Arrange
        var apiResponse = new ApiResponse<IrasApplicationResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new IrasApplicationResponse(),
            new()
        );

        _applicationsServiceClient
            .Setup(c => c.UpdateApplication(irasApplication))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.UpdateApplication(irasApplication);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IrasApplicationResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _applicationsServiceClient.Verify(c => c.UpdateApplication(irasApplication), Times.Once());
    }

    [Theory, AutoData]
    public async Task UpdateApplication_Should_Return_Failure_Response_When_Client_Returns_Failure(IrasApplicationRequest irasApplication)
    {
        // Arrange

        var apiResponse = new ApiResponse<IrasApplicationResponse>
        (
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            null,
            new()
        );

        _applicationsServiceClient
            .Setup(c => c.UpdateApplication(irasApplication))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.UpdateApplication(irasApplication);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IrasApplicationResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        _applicationsServiceClient.Verify(c => c.UpdateApplication(irasApplication), Times.Once());
    }

    [Fact]
    public async Task UpdateProjectRecordStatus_Should_Update_Status()
    {
        // Arrange
        var apiResponse = ApiResponseFactory.Success();
        var projectRecordId = "PR01";
        var status = "With sponsor";

        _applicationsServiceClient
            .Setup(c => c.UpdateProjectRecordStatus(projectRecordId, status)).ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.UpdateProjectRecordStatus(projectRecordId, status);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _applicationsServiceClient.Verify(c => c.UpdateProjectRecordStatus(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
    }
}