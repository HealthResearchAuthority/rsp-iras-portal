using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Controllers;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Services;

namespace Rsp.Portal.UnitTests.Web.Features.SponsorWorkspace.Authorisations;

public class AuthorisationsSponsorSelectorControllerTests
    : TestServiceBase<AuthorisationsSponsorSelectorController>
{
    private Mock<ISponsorUserAuthorisationService> Auth =>
        Mocker.GetMock<ISponsorUserAuthorisationService>();

    private Mock<ISponsorOrganisationService> SponsorOrg =>
        Mocker.GetMock<ISponsorOrganisationService>();

    private Mock<IRtsService> Rts =>
        Mocker.GetMock<IRtsService>();

    private AuthorisationsSponsorSelectorController Controller
    {
        get
        {
            var controller = Sut;
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }
    }

    // ------------------------------------------------------------
    // NOT AUTHORISED
    // ------------------------------------------------------------

    [Fact]
    public async Task SponsorSelector_When_NotAuthorised_Returns_FailureResult()
    {
        var sponsorOrganisationUserId = Guid.NewGuid();

        Auth.Setup(x =>
                x.AuthoriseAsync(It.IsAny<Controller>(),
                    sponsorOrganisationUserId,
                    It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(SponsorUserAuthorisationResult.Fail(new ForbidResult()));

        var result = await Controller.SponsorSelector(sponsorOrganisationUserId);

        result.ShouldBeOfType<ForbidResult>();

        SponsorOrg.Verify(
            x => x.GetAllActiveSponsorOrganisationsForEnabledUser(It.IsAny<Guid>()),
            Times.Never);
    }

    // ------------------------------------------------------------
    // SERVICE FAILURE
    // ------------------------------------------------------------

    [Fact]
    public async Task SponsorSelector_When_SponsorOrganisationService_Fails_Returns_ServiceError()
    {
        var sponsorOrganisationUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        Auth.Setup(x =>
                x.AuthoriseAsync(It.IsAny<Controller>(),
                    sponsorOrganisationUserId,
                    It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(SponsorUserAuthorisationResult.Ok(currentUserId));

        SponsorOrg.Setup(x =>
                x.GetAllActiveSponsorOrganisationsForEnabledUser(currentUserId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
            {
                StatusCode = HttpStatusCode.BadGateway
            });

        var result = await Controller.SponsorSelector(sponsorOrganisationUserId);

        result.ShouldBeAssignableTo<IActionResult>();

        Rts.Verify(x => x.GetOrganisation(It.IsAny<string>()), Times.Never);
    }

    // ------------------------------------------------------------
    // ZERO VALID ORGS
    // ------------------------------------------------------------

    [Fact]
    public async Task SponsorSelector_When_No_Valid_Orgs_Returns_Forbid()
    {
        var sponsorOrganisationUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        Auth.Setup(x =>
                x.AuthoriseAsync(It.IsAny<Controller>(),
                    sponsorOrganisationUserId,
                    It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(SponsorUserAuthorisationResult.Ok(currentUserId));

        SponsorOrg.Setup(x =>
                x.GetAllActiveSponsorOrganisationsForEnabledUser(currentUserId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<SponsorOrganisationDto>()
            });

        var result = await Controller.SponsorSelector(sponsorOrganisationUserId);

        result.ShouldBeOfType<ForbidResult>();
        Rts.Verify(x => x.GetOrganisation(It.IsAny<string>()), Times.Never);
    }

    // ------------------------------------------------------------
    // SINGLE ORG → REDIRECT
    // ------------------------------------------------------------

    [Fact]
    public async Task SponsorSelector_When_Single_Org_Redirects_To_Modifications()
    {
        var sponsorOrganisationUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        Auth.Setup(x =>
                x.AuthoriseAsync(It.IsAny<Controller>(),
                    sponsorOrganisationUserId,
                    It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(SponsorUserAuthorisationResult.Ok(currentUserId));

        var org = new SponsorOrganisationDto
        {
            RtsId = "123",
            IsActive = true,
            Users = new List<SponsorOrganisationUserDto>
            {
                new() { UserId = currentUserId, IsAuthoriser = true }
            }
        };

        SponsorOrg.Setup(x =>
                x.GetAllActiveSponsorOrganisationsForEnabledUser(currentUserId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<SponsorOrganisationDto> { org }
            });

        Rts.Setup(x => x.GetOrganisation("123"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = "Org Name" }
            });

        var result = await Controller.SponsorSelector(sponsorOrganisationUserId);

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("sws:modifications");

        redirect.RouteValues!["sponsorOrganisationUserId"]
            .ShouldBe(sponsorOrganisationUserId);

        redirect.RouteValues["rtsId"].ShouldBe("123");

        org.SponsorOrganisationName.ShouldBe("Org Name");
    }

    // ------------------------------------------------------------
    // MULTIPLE ORGS → VIEW
    // ------------------------------------------------------------

    [Fact]
    public async Task SponsorSelector_When_Multiple_Orgs_Returns_View_With_Model()
    {
        var sponsorOrganisationUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        Auth.Setup(x =>
                x.AuthoriseAsync(It.IsAny<Controller>(),
                    sponsorOrganisationUserId,
                    It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(SponsorUserAuthorisationResult.Ok(currentUserId));

        var org1 = new SponsorOrganisationDto
        {
            RtsId = "111",
            IsActive = true,
            Users = new List<SponsorOrganisationUserDto>
            {
                new() { UserId = currentUserId, IsAuthoriser = true }
            }
        };

        var org2 = new SponsorOrganisationDto
        {
            RtsId = "222",
            IsActive = true,
            Users = new List<SponsorOrganisationUserDto>
            {
                new() { UserId = currentUserId, IsAuthoriser = true }
            }
        };

        SponsorOrg.Setup(x =>
                x.GetAllActiveSponsorOrganisationsForEnabledUser(currentUserId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<SponsorOrganisationDto> { org1, org2 }
            });

        Rts.Setup(x => x.GetOrganisation("111"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = "Org 111" }
            });

        Rts.Setup(x => x.GetOrganisation("222"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = "Org 222" }
            });

        var result = await Controller.SponsorSelector(sponsorOrganisationUserId);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<AuthorisationsSponsorSelectorViewModel>();

        model.SponsorOrganisationUserId.ShouldBe(sponsorOrganisationUserId);
        model.SponsorOrganisations!.Count().ShouldBe(2);

        org1.SponsorOrganisationName.ShouldBe("Org 111");
        org2.SponsorOrganisationName.ShouldBe("Org 222");
    }
}