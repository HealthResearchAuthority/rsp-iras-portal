using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Filters;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;

namespace Rsp.IrasPortal.UnitTests.Application.Filters;

public class ModificationAuthoriseFilterTests
{
    private readonly Mock<IProjectModificationsService> _modService = new();
    private readonly Mock<ISponsorOrganisationService> _orgService = new();
    private readonly Mock<ISponsorUserAuthorisationService> _authService = new();
    private readonly Mock<ITempDataDictionaryFactory> _tempFactory = new();
    private readonly Mock<ITempDataDictionary> _tempData = new();

    private readonly ClaimsPrincipal _user;

    public ModificationAuthoriseFilterTests()
    {
        _tempFactory.Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Returns(_tempData.Object);

        _user = new ClaimsPrincipal(
            new ClaimsIdentity(new[]
            {
                new Claim("permissions", Permissions.Sponsor.Modifications_Authorise)
            }, "mock"));
    }

    private static AuthorizationFilterContext MakeAuthContext(ClaimsPrincipal? user = null)
    {
        var http = new DefaultHttpContext();
        http.User = user ?? new ClaimsPrincipal(new ClaimsIdentity());

        var actionCtx = new ActionContext(http, new RouteData(), new ControllerActionDescriptor());
        return new AuthorizationFilterContext(actionCtx, new IFilterMetadata[0]);
    }

    private static ActionExecutingContext MakeActionContext(object controller, ClaimsPrincipal user)
    {
        var http = new DefaultHttpContext();
        http.User = user;

        var actionContext = new ActionContext(
            http,
            new RouteData(),
            new ControllerActionDescriptor
            {
                RouteValues = { ["action"] = "ModificationDetails" }
            });

        return new ActionExecutingContext(
            actionContext,
            new IFilterMetadata[0],
            new Dictionary<string, object>(),
            controller);
    }

    private ModificationAuthoriseFilter MakeFilter(string permission = Permissions.Sponsor.Modifications_Authorise)
    {
        return new ModificationAuthoriseFilter(
            permission,
            _modService.Object,
            _orgService.Object,
            _authService.Object,
            _tempFactory.Object);
    }

    // ---------------------------------------------------------------
    //  AUTHORIZATION TESTS
    // ---------------------------------------------------------------

    [Fact]
    public async Task Authorization_Should_Challenge_If_User_Not_Authenticated()
    {
        var filter = MakeFilter();

        var unauthUser = new ClaimsPrincipal(new ClaimsIdentity());
        var ctx = MakeAuthContext(unauthUser);

        await filter.OnAuthorizationAsync(ctx);

        ctx.Result.ShouldBeOfType<ChallengeResult>();
    }

    [Fact]
    public async Task Authorization_Should_Allow_If_User_Has_Target_Permission()
    {
        var filter = MakeFilter();
        var ctx = MakeAuthContext(_user);

        await filter.OnAuthorizationAsync(ctx);

        ctx.Result.ShouldBeNull();
    }

    [Fact]
    public async Task Authorization_Should_Forbid_If_User_Missing_Permission()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "mock"));

        var filter = MakeFilter("SOME_PERMISSION");
        var ctx = MakeAuthContext(user);

        await filter.OnAuthorizationAsync(ctx);

        ctx.Result.ShouldBeOfType<ForbidResult>();
    }

    // ---------------------------------------------------------------
    //  ACTION EXECUTION TESTS
    // ---------------------------------------------------------------

    [Fact]
    public async Task ActionExecution_Should_BadRequest_When_RouteParams_Missing()
    {
        var filter = MakeFilter();
        var ctx = MakeActionContext(new TestController(), _user);

        await filter.OnAuthorizationAsync(MakeAuthContext(_user));

        ctx.ActionArguments.Clear();

        var nextCalled = false;
        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null);
        });

        ctx.Result.ShouldBeOfType<BadRequestObjectResult>();
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task ActionExecution_Should_Pass_When_Not_ReviseAndAuthorise()
    {
        var filter = MakeFilter();

        var controller = new TestController();
        var ctx = MakeActionContext(controller, _user);

        ctx.ActionArguments["projectRecordId"] = "abc";
        ctx.ActionArguments["projectModificationId"] = Guid.NewGuid();

        await filter.OnAuthorizationAsync(MakeAuthContext(_user));

        _modService
           .Setup(s => s.GetModification(It.IsAny<string>(), It.IsAny<Guid>()))
           .ReturnsAsync(
                        new ServiceResponse<ProjectModificationResponse>()
                           .WithContent
                           (
                               new ProjectModificationResponse
                               {
                                   Status = "SomethingElse"
                               },
                               HttpStatusCode.OK
                           )
                        );

        var nextCalled = false;

        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null);
        });

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ActionExecution_Should_Forbid_When_Not_Authorised_User()
    {
        var filter = MakeFilter();
        var controller = new TestController();
        var ctx = MakeActionContext(controller, _user);

        ctx.ActionArguments["projectRecordId"] = "abc";
        ctx.ActionArguments["projectModificationId"] = Guid.NewGuid();
        ctx.ActionArguments["sponsorOrganisationUserId"] = Guid.NewGuid();
        ctx.ActionArguments["rtsId"] = "rts1";

        await filter.OnAuthorizationAsync(MakeAuthContext(_user));

        // --- MOCK modification status = ReviseAndAuthorise ---
        _modService.Setup(s =>
                s.GetModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(
                new ServiceResponse<ProjectModificationResponse>()
                    .WithContent(
                        new ProjectModificationResponse
                        {
                            Status = ModificationStatus.ReviseAndAuthorise
                        },
                        HttpStatusCode.OK
                    )
            );

        // --- MOCK: AuthoriseWithOrganisationContextAsync -> NOT authorised ---
        _authService.Setup(a =>
                a.AuthoriseWithOrganisationContextAsync(
                    It.IsAny<Controller>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<string>()))
            .ReturnsAsync(
                SponsorUserAuthorisationResult.Fail(new ForbidResult())
            );

        var nextCalled = false;

        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null);
        });

        ctx.Result.ShouldBeOfType<ForbidResult>();
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task ActionExecution_Should_Pass_When_User_Is_Authoriser()
    {
        var filter = MakeFilter();
        var controller = new TestController();
        var ctx = MakeActionContext(controller, _user);

        ctx.ActionArguments["projectRecordId"] = "abc";
        ctx.ActionArguments["projectModificationId"] = Guid.NewGuid();
        ctx.ActionArguments["sponsorOrganisationUserId"] = Guid.NewGuid();
        ctx.ActionArguments["rtsId"] = "rts1";

        await filter.OnAuthorizationAsync(MakeAuthContext(_user));

        // --- MOCK modification status = ReviseAndAuthorise ---
        _modService.Setup(s =>
                s.GetModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(
                new ServiceResponse<ProjectModificationResponse>()
                    .WithContent(
                        new ProjectModificationResponse
                        {
                            Status = ModificationStatus.ReviseAndAuthorise
                        },
                        HttpStatusCode.OK
                    )
            );

        // --- MOCK: user authorisation successful ---
        _authService.Setup(a =>
                    a.AuthoriseWithOrganisationContextAsync(
                        It.IsAny<Controller>(),
                        It.IsAny<Guid>(),
                        It.IsAny<ClaimsPrincipal>(),
                        It.IsAny<string>()))
                .ReturnsAsync(
                    SponsorUserAuthorisationResult.Ok(Guid.NewGuid())
                );

        // --- MOCK: sponsorOrganisationUser.IsAuthoriser = true ---
        _orgService.Setup(o =>
                o.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(
                new ServiceResponse<SponsorOrganisationUserDto>()
                    .WithContent(new SponsorOrganisationUserDto
                    {
                        IsAuthoriser = true
                    }, HttpStatusCode.OK)
            );

        var nextCalled = false;

        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null);
        });

        nextCalled.ShouldBeTrue();
    }

    private class TestController : Controller
    { }
}