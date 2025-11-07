using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.SponsorWorkspace.Authorisations;

public class AuthorisationsControllerTests : TestServiceBase<AuthorisationsController>
{
    private readonly DefaultHttpContext _http;
    private readonly Guid _sponsorOrganisationUserId = Guid.NewGuid();

    public AuthorisationsControllerTests()
    {
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        _http.User = new ClaimsPrincipal(new ClaimsIdentity());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };

        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());
    }

    [Theory, AutoData]
    public async Task Authorisations_Returns_View_With_Correct_Model(GetModificationsResponse modificationResponse)
    {
        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsBySponsorOrganisationUserId(_sponsorOrganisationUserId, It.IsAny<SponsorAuthorisationsSearchRequest>(), 1, 20, nameof(ModificationsModel.SentToSponsorDate), SortDirections.Descending))
            .ReturnsAsync(serviceResponse);

        var result = await Sut.Authorisations(_sponsorOrganisationUserId);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorAuthorisationsViewModel>();

        model.ShouldNotBeNull();
        model.SponsorOrganisationUserId.ShouldBe(_sponsorOrganisationUserId);
        model.Modifications.ShouldNotBeNull();
        model.Pagination.ShouldNotBeNull();
        model.Pagination.RouteName.ShouldBe("sws:authorisations");
        model.Pagination.AdditionalParameters.ShouldContainKey("SponsorOrganisationUserId");
    }

    [Theory, AutoData]
    public async Task ApplyFilters_Invalid_ModelState_Redirects_Back(SponsorAuthorisationsViewModel model)
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("SearchTerm", "Invalid search term")
        });

        Mocker.GetMock<IValidator<SponsorAuthorisationsSearchModel>>()
            .Setup(v => v.ValidateAsync(model.Search, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await Sut.ApplyFilters(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ShouldNotBeNull();
        redirectResult.ActionName.ShouldBe(nameof(Authorisations));
        redirectResult.RouteValues["sponsorOrganisationUserId"].ShouldBe(model.SponsorOrganisationUserId);
    }
}