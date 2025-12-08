using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Infrastructure.Claims;
using Rsp.IrasPortal.Services.Extensions;
using Rsp.IrasPortal.UnitTests.TestHelpers;
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
        result.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == "applicant");
        result.Claims.Count(c => c.Type == ClaimTypes.Role).ShouldBe(2); // Only the default role
    }

    [Theory, AutoData]
    public async Task TransformAsync_Should_Add_Roles_From_UserManagementService_When_EmailClaim_Is_Present
    (
        string email,
        string firstName,
        string lastName,
        string identityProviderId,
        List<string> roles,
        User user
    )
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
            Roles = roles,
            User = user
        };

        var apiResponse = new ApiResponse<UserResponse>
        (
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

    [Theory, AutoData]
    public async Task TransformAsync_Should_Add_UserStatus_Claim_When_UserManagementService_Returns_Success
    (
        string email,
        string identityProviderId,
        User user
    )
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, identityProviderId)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var httpContext = new DefaultHttpContext()
        {
            Session = new FakeSession(),
        };

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        var userResponse = new UserResponse
        {
            Roles = [],
            User = user with { Status = IrasUserStatus.Active }
        };

        var apiResponse = ApiResponseFactory.Success(userResponse);

        var serviceResponse = apiResponse.ToServiceResponse();

        _userManagementService
            .Setup(x => x.GetUser(null, null, identityProviderId))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.TransformAsync(principal);

        // Assert
        result.Claims.ShouldContain(c =>
            c.Type == CustomClaimTypes.UserStatus &&
            c.Value == IrasUserStatus.Active);
    }

    [Theory, AutoData]
    public async Task TransformAsync_Should_Return_Principal_Without_Additional_Claims_When_User_Status_Is_Disabled
    (
        string email,
        string identityProviderId,
        User user
    )
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, identityProviderId)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var httpContext = new DefaultHttpContext()
        {
            Session = new FakeSession(),
        };

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        var userResponse = new UserResponse
        {
            Roles = ["admin", "reviewer"],
            User = user with { Status = IrasUserStatus.Disabled }
        };

        var apiResponse = new ApiResponse<UserResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            userResponse,
            new RefitSettings()
        );

        var serviceResponse = apiResponse.ToServiceResponse();

        _userManagementService
            .Setup(x => x.GetUser(null, null, identityProviderId))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.TransformAsync(principal);

        // Assert
        // Should have UserStatus claim with Disabled value
        result.Claims.ShouldContain(c =>
            c.Type == CustomClaimTypes.UserStatus &&
            c.Value == IrasUserStatus.Disabled);

        // Should have the default portal user role
        result.Claims.ShouldContain(c =>
            c.Type == ClaimTypes.Role &&
            c.Value == "iras_portal_user");

        // Should NOT have the user roles from the response since user is disabled
        result.Claims.ShouldNotContain(c =>
            c.Type == ClaimTypes.Role &&
            (c.Value == "admin" || c.Value == "reviewer"));

        // Should NOT have any other claims beyond the default role
        result.Claims
            .Count(c => c.Type == ClaimTypes.Role)
            .ShouldBe(1); // Only the default role
    }

    [Theory, AutoData]
    public async Task TransformAsync_Should_Add_UserStatus_Claim_With_Disabled_Value_For_Disabled_User
    (
        string email,
        string identityProviderId,
        User user
    )
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, identityProviderId)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var httpContext = new DefaultHttpContext()
        {
            Session = new FakeSession(),
        };

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        var userResponse = new UserResponse
        {
            Roles = [],
            User = user with { Status = IrasUserStatus.Disabled }
        };

        var apiResponse = ApiResponseFactory.Success(userResponse);

        var serviceResponse = apiResponse.ToServiceResponse();

        _userManagementService
            .Setup(x => x.GetUser(null, null, identityProviderId))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.TransformAsync(principal);

        // Assert
        var userStatusClaim = result.Claims.FirstOrDefault(c =>
            c.Type == CustomClaimTypes.UserStatus);

        userStatusClaim.ShouldNotBeNull();
        userStatusClaim.Value.ShouldBe(IrasUserStatus.Disabled);
    }

    [Theory, AutoData]
    public async Task TransformAsync_Should_UpdateUserEmailAndPhoneNumber_When_FirstLogin_And_UserFoundByIdentityProvider(
        string email,
        string identityProviderId,
        User user)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, identityProviderId),
            new(ClaimTypes.MobilePhone, "0123456789")
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var httpContext = new DefaultHttpContext()
        {
            Session = new FakeSession(),
        };

        // Set up the session to simulate first login
        httpContext.Session.SetString(SessionKeys.FirstLogin, bool.TrueString);

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Mock UserManagementService to return a success response with user found by identityProviderId
        var userResponse = new UserResponse
        {
            Roles = new List<string>(),
            User = user
        };

        var apiResponse = new ApiResponse<UserResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            userResponse,
            new RefitSettings()
        );

        var serviceResponse = apiResponse.ToServiceResponse();

        _userManagementService
            .Setup(x => x.GetUser(null, null, identityProviderId))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.TransformAsync(principal);

        // Assert
        _userManagementService.Verify(x => x.UpdateUserEmailAndPhoneNumber(It.Is<User>(u => u.Email == user.Email), email, "0123456789"), Times.Once);
        _userManagementService.Verify(x => x.UpdateLastLogin(email), Times.Once);
    }

    [Theory, AutoData]
    public async Task TransformAsync_Should_UpdateUserIdentityProviderId_When_FirstLogin_And_NotFoundByIdentityProvider_But_FoundByEmail(
        string email,
        string identityProviderId,
        User user)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, identityProviderId),
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var httpContext = new DefaultHttpContext()
        {
            Session = new FakeSession(),
        };

        // Set up the session to simulate first login
        httpContext.Session.SetString(SessionKeys.FirstLogin, bool.TrueString);

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // First call by identityProviderId returns NotFound
        var notFoundApiResponse = new ApiResponse<UserResponse>(new HttpResponseMessage(HttpStatusCode.NotFound), null, new RefitSettings());
        var notFoundServiceResponse = notFoundApiResponse.ToServiceResponse();

        _userManagementService
            .Setup(x => x.GetUser(null, null, identityProviderId))
            .ReturnsAsync(notFoundServiceResponse);

        // Second call by email returns success
        var userResponseByEmail = new UserResponse
        {
            Roles = new List<string>(),
            User = user
        };

        var apiResponseByEmail = new ApiResponse<UserResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            userResponseByEmail,
            new RefitSettings()
        );

        var serviceResponseByEmail = apiResponseByEmail.ToServiceResponse();

        _userManagementService
            .Setup(x => x.GetUser(null, email, null))
            .ReturnsAsync(serviceResponseByEmail);

        // Act
        var result = await Sut.TransformAsync(principal);

        // Assert
        _userManagementService.Verify(x => x.UpdateUserIdentityProviderId(It.Is<User>(u => u.Email == user.Email), identityProviderId), Times.Once);
        _userManagementService.Verify(x => x.UpdateLastLogin(email), Times.Once);
    }

    [Theory, AutoData]
    public async Task TransformAsync_Should_Add_ApplicantRole_And_RequestProfileCompletion_When_FirstLogin_And_User_NotFoundByAny(
        string email,
        string identityProviderId)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, identityProviderId),
            new(ClaimTypes.MobilePhone, "0123456789")
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var httpContext = new DefaultHttpContext()
        {
            Session = new FakeSession(),
        };

        // Set up the session to simulate first login
        httpContext.Session.SetString(SessionKeys.FirstLogin, bool.TrueString);

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Both calls return NotFound
        var notFoundApiResponse = new ApiResponse<UserResponse>(new HttpResponseMessage(HttpStatusCode.NotFound), null, new RefitSettings());
        var notFoundServiceResponse = notFoundApiResponse.ToServiceResponse();

        _userManagementService
            .Setup(x => x.GetUser(null, null, identityProviderId))
            .ReturnsAsync(notFoundServiceResponse);

        _userManagementService
            .Setup(x => x.GetUser(null, email, null))
            .ReturnsAsync(notFoundServiceResponse);

        // Act
        var result = await Sut.TransformAsync(principal);

        // Assert
        result.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == Roles.Applicant);

        var context = httpContext;
        context.Items.ShouldContainKey(ContextItemKeys.RequireProfileCompletion);
        context.Items[ContextItemKeys.Email].ShouldBe(email);
        context.Items["telephoneNumber"].ShouldBe("0123456789");
        context.Items["identityProviderId"].ShouldBe(identityProviderId);

        // FirstLogin session should be removed
        context.Session.GetString(SessionKeys.FirstLogin).ShouldBeNull();
    }

    [Theory, AutoData]
    public async Task TransformAsync_Should_Add_ApplicantRole_When_RequestQuery_RequiresProfileCreation(
        string email)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var httpContext = new DefaultHttpContext()
        {
            Session = new FakeSession(),
        };

        httpContext.Request.QueryString = new QueryString("?requireProfileCreation=1");

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Ensure userManagementService returns NotFound so we reach the query branch
        var notFoundApiResponse = new ApiResponse<UserResponse>(new HttpResponseMessage(HttpStatusCode.NotFound), null, new RefitSettings());
        var notFoundServiceResponse = notFoundApiResponse.ToServiceResponse();

        _userManagementService
            .Setup(x => x.GetUser(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(notFoundServiceResponse);

        // Act
        var result = await Sut.TransformAsync(principal);

        // Assert
        result.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == Roles.Applicant);
    }
}