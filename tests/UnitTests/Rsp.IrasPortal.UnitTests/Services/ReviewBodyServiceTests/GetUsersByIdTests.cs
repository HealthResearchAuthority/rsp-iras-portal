using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ReviewBodyServiceTests;

public class GetUsersByIdTests : TestServiceBase<ReviewBodyService>
{
    [Theory]
    [AutoData]
    public async Task GetUsersById_Should_Return_Failure_Response_When_Client_Returns_Failure(
        IEnumerable<string> ids,
        string? searchQuery,
        int pageNumber,
        int pageSize)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<UsersResponse>>(
           apiResponse => !apiResponse.IsSuccessStatusCode &&
                          apiResponse.StatusCode == HttpStatusCode.NotFound);

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.GetUsersById(ids, searchQuery, pageNumber, pageSize))
            .ReturnsAsync(apiResponse);

        var sut = new UserManagementService(client.Object);

        // Act
        var result = await sut.GetUsersByIds(ids, searchQuery, pageNumber, pageSize);

        // Assert
        result.ShouldBeOfType<ServiceResponse<UsersResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        client.Verify(c => c.GetUsersById(ids, searchQuery, pageNumber, pageSize), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetUsersByIdTests_Should_Return_Success_Response_When_Client_Returns_Success(
        IEnumerable<string> ids,
        string? searchQuery,
        int pageNumber,
        int pageSize)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<UsersResponse>>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.GetUsersById(ids, searchQuery, pageNumber, pageSize))
            .ReturnsAsync(apiResponse);

        var sut = new UserManagementService(client.Object);

        // Act
        var result = await sut.GetUsersByIds(ids, searchQuery, pageNumber, pageSize);

        // Assert
        result.ShouldBeOfType<ServiceResponse<UsersResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.GetUsersById(ids, searchQuery, pageNumber, pageSize), Times.Once());
    }
}