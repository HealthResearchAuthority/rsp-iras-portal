using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Infrastructure.Claims;
using Rsp.IrasPortal.Services.Extensions;
using Claim = System.Security.Claims.Claim;

namespace Rsp.IrasPortal.UnitTests.Infrastructure.CustomClaimsTransformationTests;

public class TransformAsyncTests : TestServiceBase<CustomClaimsTransformation>
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<IUserManagementService> _userManagementService;
    private readonly Mock<IOptionsSnapshot<AppSettings>> _appSettings;

    public TransformAsyncTests()
    {
        _httpContextAccessor = Mocker.GetMock<IHttpContextAccessor>();
        _userManagementService = Mocker.GetMock<IUserManagementService>();
        _appSettings = Mocker.GetMock<IOptionsSnapshot<AppSettings>>();

        // Mock AppSettings
        _appSettings
            .Setup(x => x.Value)
            .Returns(new AppSettings
            {
                AuthSettings = new AuthSettings { ClientId = "test-client-id" }
            });
    }

    [Fact]
    public async Task TransformAsync_Should_Return_Original_Principal_When_EmailClaim_Is_Missing()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await Sut.TransformAsync(principal);

        // Assert
        result.ShouldBe(principal);
        result.Claims.ShouldNotContain(c => c.Type == ClaimTypes.Role && c.Value == "iras_portal_user");
    }

    [Theory, AutoData]
    public async Task TransformAsync_Should_Add_Default_Role_When_EmailClaim_Is_Present(string email, string identityProviderId)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, identityProviderId)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Mock HttpContext
        var httpContext = new DefaultHttpContext()
        {
            Session = new FakeSession(),
        };

        // Set up the session to simulate first login
        httpContext.Session.SetString(SessionKeys.FirstLogin, bool.TrueString);

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Mock UserManagementService to return a failure response
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        var apiResponse = new ApiResponse<UserResponse>(httpResponse, null, new());

        var serviceResponse = apiResponse.ToServiceResponse();

        _userManagementService
            .Setup(c => c.GetUser(null, email, null))
            .ReturnsAsync(serviceResponse);

        _userManagementService
           .Setup(c => c.GetUser(null, null, identityProviderId))
           .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.TransformAsync(principal);

        // Assert
        result.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == "iras_portal_user");
        result.Claims.Count(c => c.Type == ClaimTypes.Role).ShouldBe(1); // Only the default role
    }

    [Theory, AutoData]
    public async Task TransformAsync_Should_Add_Roles_From_UserManagementService_When_EmailClaim_Is_Present(
        string email, string firstName, string lastName, string identityProviderId, List<string> roles)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, identityProviderId),
            new(ClaimTypes.GivenName, firstName),
            new(ClaimTypes.Surname, lastName)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Mock HttpContext
        var httpContext = new DefaultHttpContext()
        {
            Session = new FakeSession(),
        };

        // Set up the session to simulate first login
        httpContext.Session.SetString(SessionKeys.FirstLogin, bool.TrueString);

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Mock UserManagementService to return a success response with roles
        var userResponse = new UserResponse
        {
            Roles = roles
        };
        var apiResponse = new ApiResponse<UserResponse>(
            new HttpResponseMessage(HttpStatusCode.OK),
            userResponse,
            new RefitSettings()
        );

        var serviceResponse = apiResponse.ToServiceResponse();

        _userManagementService
            .Setup(x => x.GetUser(null, email, null))
            .ReturnsAsync(serviceResponse);

        _userManagementService
           .Setup(x => x.GetUser(null, null, identityProviderId))
           .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.TransformAsync(principal);

        // Assert
        result.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == "iras_portal_user");

        foreach (var role in roles)
        {
            result.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == role);
        }

        result.Claims
            .Count(c => c.Type == ClaimTypes.Role)
            .ShouldBe(roles.Count + 1); // Default role + roles from user management
    }
}