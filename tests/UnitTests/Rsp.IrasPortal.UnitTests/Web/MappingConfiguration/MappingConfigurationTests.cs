using Mapster;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.MappingConfigurators;

namespace Rsp.IrasPortal.UnitTests.Web.MappingConfigurationTests;

public class MappingConfigurationTests : TestServiceBase
{
    public MappingConfigurationTests()
    {
        // Ensure mapping is configured before tests run
        MappingConfiguration.Configure();
    }

    [Fact]
    public void UserViewModel_To_UpdateUserRequest_Should_Map_Country_Correctly()
    {
        // Arrange
        var viewModel = new UserViewModel
        {
            Country = new List<string> { "USA", "Canada" }
        };

        // Act
        var result = viewModel.Adapt<UpdateUserRequest>();

        // Assert
        result.Country.ShouldBe("USA,Canada");
    }

    [Fact]
    public void UserViewModel_To_CreateUserRequest_Should_Map_Country_Correctly()
    {
        // Arrange
        var viewModel = new UserViewModel
        {
            Country = new List<string> { "UK", "Germany" }
        };

        // Act
        var result = viewModel.Adapt<CreateUserRequest>();

        // Assert
        result.Country.ShouldBe("UK,Germany");
    }

    [Fact]
    public void UserViewModel_With_Null_Country_Should_Map_To_Null()
    {
        // Arrange
        var viewModel = new UserViewModel
        {
            Country = null
        };

        // Act
        var updateResult = viewModel.Adapt<UpdateUserRequest>();
        var createResult = viewModel.Adapt<CreateUserRequest>();

        // Assert
        updateResult.Country.ShouldBeNull();
        createResult.Country.ShouldBeNull();
    }
}