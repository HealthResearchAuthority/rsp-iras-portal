using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.UserManagementServiceTests;

public class UpdateUserAccessTests : TestServiceBase<UserManagementService>
{
    [Theory, AutoData]
    public async Task UpdateUserAccess_Should_Return_Success_Response_When_Client_Returns_Success(
        string userEmail,
        IEnumerable<string> accessRequired,
        IEnumerable<string> existingUserClaims
        )
    {
        // Arrange

        var getClaimsApiResponse = Mock.Of<IApiResponse<IEnumerable<UserClaimDto>>>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK &&
                apiResponse.Content == existingUserClaims.Select(x => new UserClaimDto(UserClaimTypes.AccessRequired, x))
        );

        var removeClaimsApiResponse = Mock.Of<IApiResponse>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var addClaimsApiResponse = Mock.Of<IApiResponse>
       (
           apiResponse =>
               apiResponse.IsSuccessStatusCode &&
               apiResponse.StatusCode == HttpStatusCode.OK
       );

        var client = Mocker.GetMock<IUserManagementServiceClient>();

        client
            .Setup(c => c.GetUserClaims(null, userEmail))
            .ReturnsAsync(getClaimsApiResponse);

        client
            .Setup(c => c.RemoveUserClaims(It.IsAny<UserClaimsRequest>()))
            .ReturnsAsync(removeClaimsApiResponse);

        client
            .Setup(c => c.AddUserClaims(It.IsAny<UserClaimsRequest>()))
            .ReturnsAsync(addClaimsApiResponse);

        // Act
        var result = await Sut.UpdateUserAccess(userEmail, accessRequired);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.GetUserClaims(null, userEmail), Times.Once());
        client.Verify(c => c.RemoveUserClaims(It.IsAny<UserClaimsRequest>()), Times.Once());
        client.Verify(c => c.AddUserClaims(It.IsAny<UserClaimsRequest>()), Times.Once());
    }
}