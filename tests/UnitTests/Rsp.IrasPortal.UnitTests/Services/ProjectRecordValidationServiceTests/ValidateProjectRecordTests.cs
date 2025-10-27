using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectRecordValidationServiceTests;

public class ValidateProjectRecordTests : TestServiceBase<ProjectRecordValidationService>
{
    private readonly Mock<IProjectRecordValidationClient> _validationClient;

    public ValidateProjectRecordTests()
    {
        _validationClient = Mocker.GetMock<IProjectRecordValidationClient>();
    }

    [Theory, AutoData]
    public async Task ValidateProjectRecord_Should_Return_Success_Response_When_Client_Returns_Success(int irasId)
    {
        // Arrange
        var apiResponse = ApiResponseFactory.Success(new ProjectRecordValidationResponse());

        _validationClient
        .Setup(c => c.ValidateProjectRecord(irasId))
        .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.ValidateProjectRecord(irasId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ProjectRecordValidationResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _validationClient.Verify(c => c.ValidateProjectRecord(irasId), Times.Once());
    }

    [Theory, AutoData]
    public async Task ValidateProjectRecord_Should_Return_Failure_Response_When_Client_Returns_Failure(int irasId)
    {
        // Arrange
        var apiResponse = ApiResponseFactory.Failure<ProjectRecordValidationResponse>(HttpStatusCode.NotFound, "Not Found", string.Empty);

        _validationClient
        .Setup(c => c.ValidateProjectRecord(irasId))
        .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.ValidateProjectRecord(irasId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ProjectRecordValidationResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        _validationClient.Verify(c => c.ValidateProjectRecord(irasId), Times.Once());
    }
}