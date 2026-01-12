using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationServiceTests;

public class DeleteModificationTests : TestServiceBase<ProjectModificationsService>
{
    [Theory]
    [AutoData]
    public async Task DeleteModification_Should_Return_Success_Response_When_Client_Returns_Success
    (
        string projectRecordId,
        Guid modificationId,
        string payload
    )
    {
        // Arrange
        var apiResponse = new ApiResponse<string>(
            new HttpResponseMessage(HttpStatusCode.OK),
            payload,
            new RefitSettings());

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.DeleteModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.DeleteModification(projectRecordId, modificationId);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        projectModificationsServiceClient.Verify(c => c.DeleteModification(projectRecordId, modificationId), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task DeleteModification_Should_Return_Failure_Response_When_Client_Returns_Failure
    (
        string projectRecordId,
        Guid modificationId,
        string payload
    )
    {
        // Arrange
        var apiResponse = new ApiResponse<string>(
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            payload,
            new RefitSettings());

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.DeleteModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.DeleteModification(projectRecordId, modificationId);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        projectModificationsServiceClient.Verify(c => c.DeleteModification(projectRecordId, modificationId), Times.Once);
    }
}