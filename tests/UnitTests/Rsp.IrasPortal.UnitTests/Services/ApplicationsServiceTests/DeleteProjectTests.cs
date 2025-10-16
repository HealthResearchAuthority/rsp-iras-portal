using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationsServiceTests;

public class DeleteProjectTests : TestServiceBase<ApplicationsService>
{
    [Fact]
    public async Task DeleteProject_ShouldReturnSuccess_WhenApiReturnsOk()
    {
        // Arrange
        var apiResponse = ApiResponseFactory.Success();
        Mocker
            .GetMock<IApplicationsServiceClient>()
            .Setup(c => c.DeleteProject("project-123"))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.DeleteProject("project-123");

        // Assert
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteProject_ShouldReturnError_WhenApiReturnsError()
    {
        // Arrange
        var apiResponse = ApiResponseFactory.Failure(HttpStatusCode.InternalServerError, "Internal Server Error");
        Mocker
            .GetMock<IApplicationsServiceClient>()
            .Setup(c => c.DeleteProject("project-123"))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.DeleteProject("project-123");

        // Assert
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.ReasonPhrase.ShouldBe("Internal Server Error");
    }
}