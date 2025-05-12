using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ReviewBodyServiceTests;

public class SearchReviewBodyUsersTests : TestServiceBase<ReviewBodyService>
{
    [Theory]
    [AutoData]
    public async Task SearchReviewBodyUsers_Should_Return_Failure_Response_When_Client_Returns_Failure(
        string searchQuery,
        int pageSize,
        int pageNumber)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<UsersResponse>>(
           apiResponse => !apiResponse.IsSuccessStatusCode &&
                          apiResponse.StatusCode == HttpStatusCode.NotFound);

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.SearchUsers(searchQuery, null, pageNumber, pageSize))
            .ReturnsAsync(apiResponse);

        var sut = new UserManagementService(client.Object);

        // Act
        var result = await sut.SearchUsers(searchQuery, null, pageNumber, pageSize);

        // Assert
        result.ShouldBeOfType<ServiceResponse<UsersResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        client.Verify(c => c.SearchUsers(searchQuery, null, pageNumber, pageSize), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task SearchReviewBodyUsers_Should_Return_Success_Response_When_Client_Returns_Success(
        string searchQuery,
        int pageSize,
        int pageNumber)
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
            .Setup(c => c.SearchUsers(searchQuery, null, pageNumber, pageSize))
            .ReturnsAsync(apiResponse);

        var sut = new UserManagementService(client.Object);

        // Act
        var result = await sut.SearchUsers(searchQuery, null, pageNumber, pageSize);

        // Assert
        result.ShouldBeOfType<ServiceResponse<UsersResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.SearchUsers(searchQuery, null, pageNumber, pageSize), Times.Once());
    }
}