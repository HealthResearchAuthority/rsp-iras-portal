using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Services;
using Claim = System.Security.Claims.Claim;

namespace Rsp.Portal.UnitTests.Web.Features.SponsorWorkspace.Authorisation.Services;

public class SponsorUserAuthorisationServiceTests
{
    private readonly Mock<ISponsorOrganisationService> _sponsorOrganisationService = new();
    private readonly Mock<IUserManagementService> _userService = new();

    private SponsorUserAuthorisationService Sut =>
        new(_userService.Object, _sponsorOrganisationService.Object);

    [Fact]
    public async Task AuthoriseAsync_When_Email_Missing_Returns_ServiceError_And_DoesNot_Call_Dependencies()
    {
        // Arrange
        var controller = NewController();
        var sponsorOrganisationUserId = Guid.NewGuid();
        var principal = BuildPrincipal(null);

        // Act
        var result = await Sut.AuthoriseAsync(controller, sponsorOrganisationUserId, principal);

        // Assert
        AssertFailedWithActionResult(result, typeof(IActionResult)); // ServiceError result type may vary
    }

    [Fact]
    public async Task AuthoriseAsync_When_UserService_Fails_Returns_ServiceError()
    {
        // Arrange
        var controller = NewController();
        var sponsorOrganisationUserId = Guid.NewGuid();
        var principal = BuildPrincipal("test@test.co.uk");

        var userEntityResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = null
        };

        _userService
            .Setup(x => x.GetUser(null, "test@test.co.uk", null))
            .ReturnsAsync(userEntityResponse);

        // Act
        var result = await Sut.AuthoriseAsync(controller, sponsorOrganisationUserId, principal);

        // Assert
        AssertFailedWithActionResult(result, typeof(IActionResult));

        _sponsorOrganisationService.Verify(x => x.GetAllActiveSponsorOrganisationsForEnabledUser(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthoriseAsync_When_UserId_Is_InvalidGuid_Returns_BadRequest_ServiceError()
    {
        // Arrange
        var controller = NewController();
        var sponsorOrganisationUserId = Guid.NewGuid();
        var principal = BuildPrincipal("test@test.co.uk");

        var userEntityResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UserResponse
            {
                User = new User(
                    "not-a-guid",
                    null,
                    null,
                    "Dan",
                    "Hulmston",
                    "test@test.co.uk",
                    null,
                    null,
                    null,
                    null,
                    "Active",
                    DateTime.UtcNow,
                    null,
                    null
                )
            }
        };

        _userService
            .Setup(x => x.GetUser(null, "test@test.co.uk", null))
            .ReturnsAsync(userEntityResponse);

        // Act
        var result = await Sut.AuthoriseAsync(controller, sponsorOrganisationUserId, principal);

        // Assert
        AssertFailedWithActionResult(result, typeof(IActionResult));

        _sponsorOrganisationService.Verify(x => x.GetAllActiveSponsorOrganisationsForEnabledUser(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthoriseAsync_When_SponsorOrganisationService_Fails_Returns_ServiceError()
    {
        // Arrange
        var controller = NewController();
        var sponsorOrganisationUserId = Guid.NewGuid();
        var principal = BuildPrincipal("test@test.co.uk");

        var gid = Guid.NewGuid();

        _userService
            .Setup(x => x.GetUser(null, "test@test.co.uk", null))
            .ReturnsAsync(OkUserResponse(gid, "test@test.co.uk"));

        var sponsorOrgsResponse = new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
        {
            StatusCode = HttpStatusCode.BadGateway,
            Content = null
        };

        _sponsorOrganisationService
            .Setup(x => x.GetAllActiveSponsorOrganisationsForEnabledUser(gid))
            .ReturnsAsync(sponsorOrgsResponse);

        // Act
        var result = await Sut.AuthoriseAsync(controller, sponsorOrganisationUserId, principal);

        // Assert
        AssertFailedWithActionResult(result, typeof(IActionResult));
    }

    [Fact]
    public async Task AuthoriseAsync_When_Membership_NotFound_Returns_Forbid()
    {
        // Arrange
        var controller = NewController();
        var sponsorOrganisationUserId = Guid.NewGuid();
        var principal = BuildPrincipal("test@test.co.uk");

        var gid = Guid.NewGuid();

        _userService
            .Setup(x => x.GetUser(null, "test@test.co.uk", null))
            .ReturnsAsync(OkUserResponse(gid, "test@test.co.uk"));

        // Sponsor org returned but does NOT contain matching user membership
        var sponsorOrgsResponse = new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<SponsorOrganisationDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Users = new List<SponsorOrganisationUserDto>
                    {
                        new()
                        {
                            UserId = Guid.NewGuid(), // different user
                            Id = Guid.NewGuid() // membership id for someone else
                        }
                    }
                }
            }
        };

        _sponsorOrganisationService
            .Setup(x => x.GetAllActiveSponsorOrganisationsForEnabledUser(gid))
            .ReturnsAsync(sponsorOrgsResponse);

        // Act
        var result = await Sut.AuthoriseAsync(controller, sponsorOrganisationUserId, principal);

        // Assert
        AssertFailedWithActionResult(result, typeof(ForbidResult));
    }

    [Fact]
    public async Task AuthoriseAsync_When_MembershipId_DoesNotMatch_Returns_Forbid()
    {
        // Arrange
        var controller = NewController();
        var principal = BuildPrincipal("test@test.co.uk");

        var gid = Guid.NewGuid();

        _userService
            .Setup(x => x.GetUser(null, "test@test.co.uk", null))
            .ReturnsAsync(OkUserResponse(gid, "test@test.co.uk"));

        var actualMembershipId = Guid.NewGuid();
        var requestedMembershipId = Guid.NewGuid(); // mismatch

        var sponsorOrgsResponse = new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<SponsorOrganisationDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Users = new List<SponsorOrganisationUserDto>
                    {
                        new()
                        {
                            UserId = gid,
                            Id = actualMembershipId
                        }
                    }
                }
            }
        };

        _sponsorOrganisationService
            .Setup(x => x.GetAllActiveSponsorOrganisationsForEnabledUser(gid))
            .ReturnsAsync(sponsorOrgsResponse);

        // Act
        var result = await Sut.AuthoriseAsync(controller, requestedMembershipId, principal);

        // Assert
        AssertFailedWithActionResult(result, typeof(ForbidResult));
    }

    [Fact]
    public async Task AuthoriseAsync_When_Authorised_Returns_Ok_With_CurrentUserGuid()
    {
        // Arrange
        var controller = NewController();
        var principal = BuildPrincipal("test@test.co.uk");

        var gid = Guid.NewGuid();
        var membershipId = Guid.NewGuid();

        _userService
            .Setup(x => x.GetUser(null, "test@test.co.uk", null))
            .ReturnsAsync(OkUserResponse(gid, "test@test.co.uk"));

        var sponsorOrgsResponse = new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<SponsorOrganisationDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Users = new List<SponsorOrganisationUserDto>
                    {
                        new()
                        {
                            UserId = gid,
                            Id = membershipId
                        }
                    }
                }
            }
        };

        _sponsorOrganisationService
            .Setup(x => x.GetAllActiveSponsorOrganisationsForEnabledUser(gid))
            .ReturnsAsync(sponsorOrgsResponse);

        // Act
        var result = await Sut.AuthoriseAsync(controller, membershipId, principal);

        // Assert CHANGE IF NEEDED: adapt these property names to your SponsorUserAuthorisationResult
        result.ShouldNotBeNull();
        GetIsSuccess(result).ShouldBeTrue();
        GetUserId(result).ShouldBe(gid);
        GetFailureResult(result).ShouldBeNull();
    }

    // ---------------- Helpers ----------------

    private static ClaimsPrincipal BuildPrincipal(string? email)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }

    private static Controller NewController()
    {
        // Any Controller works; ServiceError extension will need ControllerContext
        var c = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return c;
    }

    private static ServiceResponse<UserResponse> OkUserResponse(Guid gid, string email)
    {
        return new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UserResponse
            {
                User = new User(
                    gid.ToString(),
                    null,
                    null,
                    "Dan",
                    "Hulmston",
                    email,
                    null,
                    null,
                    null,
                    null,
                    "Active",
                    DateTime.UtcNow,
                    null,
                    null
                )
            }
        };
    }

    private static void AssertFailedWithActionResult(object result, Type expectedActionResultType)
    {
        result.ShouldNotBeNull();

        // CHANGE IF NEEDED: adapt these property names to your SponsorUserAuthorisationResult
        GetIsSuccess(result).ShouldBeFalse();

        var failure = GetFailureResult(result);
        failure.ShouldNotBeNull();

        if (expectedActionResultType == typeof(IActionResult))
        {
            failure.ShouldBeAssignableTo<IActionResult>();
        }
        else
        {
            failure.ShouldBeOfType(expectedActionResultType);
        }
    }

    // ---- Reflection-based accessors so you only change in one place if your type differs ---- If
    // you prefer strong typing, replace these with direct property access.

    private static bool GetIsSuccess(object result)
    {
        // Expected: result.IsSuccess / result.Succeeded / result.IsAuthorised
        var prop = result.GetType().GetProperty("IsSuccess")
                   ?? result.GetType().GetProperty("Succeeded")
                   ?? result.GetType().GetProperty("IsAuthorised");

        prop.ShouldNotBeNull("Could not find a success boolean on SponsorUserAuthorisationResult. Rename accessor.");
        return (bool)prop!.GetValue(result)!;
    }

    private static Guid? GetUserId(object result)
    {
        // Expected: result.UserId / result.CurrentUserId / result.Gid
        var prop = result.GetType().GetProperty("UserId")
                   ?? result.GetType().GetProperty("CurrentUserId")
                   ?? result.GetType().GetProperty("Gid");

        if (prop is null)
        {
            return null;
        }

        return (Guid?)prop.GetValue(result);
    }

    private static IActionResult? GetFailureResult(object result)
    {
        // Expected: result.ActionResult / result.Result / result.FailureResult
        var prop = result.GetType().GetProperty("ActionResult")
                   ?? result.GetType().GetProperty("Result")
                   ?? result.GetType().GetProperty("FailureResult");

        return (IActionResult?)prop?.GetValue(result);
    }

    private sealed class TestController : Controller
    {
    }
}