using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.UserManagementServiceTests;

public class UpdateUserTests : TestServiceBase<UserManagementService>
{
    [Theory, AutoData]
    public async Task UpdateUser_Should_Return_Success_Response_When_Client_Returns_Success(string originalEmail,
        string title,
        string firstName,
        string lastName,
        string email,
        string? jobTitle,
        string? organisation,
        string? telephone,
        string? country,
        string status,
        DateTime? lastUpdated)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.UpdateUser(originalEmail, It.IsAny<User>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.UpdateUser(originalEmail, title, firstName, lastName, email, jobTitle, organisation, telephone, country, status, lastUpdated);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.UpdateUser(originalEmail, It.IsAny<User>()), Times.Once());
    }

    [Theory, AutoData]
    public async Task UpdateUser_Should_Return_Failure_Response_When_Client_Returns_Failure(string originalEmail,
        string title,
        string firstName,
        string lastName,
        string email,
        string? jobTitle,
        string? organisation,
        string? telephone,
        string? country,
        string status,
        DateTime? lastUpdated)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>
        (
            apiResponse => !
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.NotFound);

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.UpdateUser(originalEmail, It.IsAny<User>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.UpdateUser(originalEmail, title, firstName, lastName, email, jobTitle, organisation, telephone, country, status, lastUpdated);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        client.Verify(c => c.UpdateUser(originalEmail, It.IsAny<User>()), Times.Once());
    }
}