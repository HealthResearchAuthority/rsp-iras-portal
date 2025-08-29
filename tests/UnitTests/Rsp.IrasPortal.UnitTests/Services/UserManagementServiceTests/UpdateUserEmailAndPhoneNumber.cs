using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.UserManagementServiceTests;

public class UpdateUserEmailAndPhoneNumberTests : TestServiceBase<UserManagementService>
{
    [Theory, AutoData]
    public async Task Do_Not_Update_When_No_Changes_Detected(User existingUser)
    {
        // setup
        // use same email and phone number as existing user to check that no updates are done to the user
        var sameEmailParameter = existingUser.Email;
        var sameTelephoneParameter = existingUser.Telephone;

        // execute
        var result = await Sut.UpdateUserEmailAndPhoneNumber(existingUser, sameEmailParameter, sameTelephoneParameter);

        // verify that no updates have been done
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Theory, AutoData]
    public async Task Update_When_Changes_Detected(User existingUser, string newEmail, string newTelephone)
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
        var result = await Sut.UpdateUserEmailAndPhoneNumber(existingUser, newEmail, newTelephone);

        // verify that no updates have been done
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}