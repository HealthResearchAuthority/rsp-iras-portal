using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests.UserManagement;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.UserManagementServiceTests;

public class CreateUserTests : TestServiceBase<UserManagementService>
{
    [Theory, AutoData]
    public async Task CreateUser_Should_Return_Success_Response_When_Client_Returns_Success(string title,
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
            .Setup(c => c.CreateUser(It.IsAny<User>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateUser(new CreateUserRequest
        {
            Title = title,
            GivenName = firstName,
            FamilyName = lastName,
            Email = email,
            JobTitle = jobTitle,
            Organisation = organisation,
            Telephone = telephone,
            Country = country,
            Status = IrasUserStatus.Active,
            LastUpdated = DateTime.UtcNow
        });

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.CreateUser(It.IsAny<User>()), Times.Once());
    }

    [Theory, AutoData]
    public async Task CreateUser_Should_Return_Failure_Response_When_Client_Returns_Failure(string title,
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
                apiResponse.StatusCode == HttpStatusCode.BadRequest
        );

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.CreateUser(It.IsAny<User>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateUser(new CreateUserRequest
        {
            Title = title,
            GivenName = firstName,
            FamilyName = lastName,
            Email = email,
            JobTitle = jobTitle,
            Organisation = organisation,
            Telephone = telephone,
            Country = country,
            Status = IrasUserStatus.Active,
            LastUpdated = DateTime.UtcNow
        });

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest
        );

        // Verify
        client.Verify(c => c.CreateUser(It.IsAny<User>()), Times.Once());
    }
}