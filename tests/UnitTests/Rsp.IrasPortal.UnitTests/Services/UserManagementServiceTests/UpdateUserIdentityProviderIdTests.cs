using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.UserManagementServiceTests;

public class UpdateUserIdentityProviderIdTests : TestServiceBase<UserManagementService>
{
    [Theory, AutoData]
    public async Task Throw_Error_When_Parameter_Is_Null(User existingUser)
    {
        // execute
        var result = await Sut.UpdateUserIdentityProviderId(existingUser, string.Empty);

        // verify
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory, AutoData]
    public async Task Execute_When_Parameter_Is_Fine(User existingUser, string identityProviderId)
    {
        // setup
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var service = Mocker.GetMock<IUserManagementServiceClient>();
        service
            .Setup(c => c.UpdateUser(It.IsAny<string>(), It.IsAny<User>()))
            .ReturnsAsync(apiResponse);

        // execute
        var result = await Sut.UpdateUserIdentityProviderId(existingUser, identityProviderId);

        // verify
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}