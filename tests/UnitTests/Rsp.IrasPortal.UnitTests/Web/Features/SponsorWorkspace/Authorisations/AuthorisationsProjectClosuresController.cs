using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Controllers;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.SponsorWorkspace.Authorisations;

public class AuthorisationsProjectClosuresControllerTests
    : TestServiceBase<AuthorisationsProjectClosuresController>
{
    private readonly DefaultHttpContext _http;
    private readonly Guid _sponsorOrganisationUserId = Guid.NewGuid();

    public AuthorisationsProjectClosuresControllerTests()
    {
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        _http.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };

        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());
    }

    [Theory]
    [AutoData]
    public async Task ProjectClosures_Returns_View_With_Correct_Model(ProjectClosuresSearchResponse closuresResponse, List<User> users)
    {
        // arrange at least 1 matching user id.
        closuresResponse.ProjectClosures.First().UserId = users[0].Id;

        var serviceResponse = new ServiceResponse<ProjectClosuresSearchResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = closuresResponse
        };

        Mocker.GetMock<IProjectClosuresService>()
            .Setup(s => s.GetProjectClosuresBySponsorOrganisationUserId(
                _sponsorOrganisationUserId,
                It.IsAny<ProjectClosuresSearchRequest>(),
                1,
                20,
                nameof(ProjectClosuresModel.SentToSponsorDate),
                SortDirections.Descending))
            .ReturnsAsync(serviceResponse);

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse { Users = users }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(
                It.IsAny<IEnumerable<string>>(),
                null,
                1,
                It.IsAny<int>()))
            .ReturnsAsync(usersResponse);

        // Act
        var result = await Sut.ProjectClosures(_sponsorOrganisationUserId);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeAssignableTo<ProjectClosuresViewModel>();

        model.ShouldNotBeNull();
        model.SponsorOrganisationUserId.ShouldBe(_sponsorOrganisationUserId);

        model.ProjectRecords.ShouldNotBeNull();
        model.ProjectRecords.Count().ShouldBe(closuresResponse.ProjectClosures.Count());

        model.ProjectRecords.First(r => r.UserId == users[0].Id).UserEmail.ShouldBe(users[0].Email);

        model.Pagination.ShouldNotBeNull();
        model.Pagination.RouteName.ShouldBe("sws:projectclosures");
        model.Pagination.AdditionalParameters.ShouldContainKey("SponsorOrganisationUserId");
        model.Pagination.AdditionalParameters["SponsorOrganisationUserId"].ShouldBe(_sponsorOrganisationUserId.ToString());
        model.Pagination.SortField.ShouldBe(nameof(ProjectClosuresModel.SentToSponsorDate));
        model.Pagination.SortDirection.ShouldBe(SortDirections.Descending);
    }

    [Theory]
    [AutoData]
    public async Task ApplyProjectClosuresFilters_Invalid_ModelState_Redirects_Back(ProjectClosuresViewModel model)
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure(nameof(ProjectClosuresSearchModel.SearchTerm), "Invalid search term")
        });

        Mocker.GetMock<IValidator<ProjectClosuresSearchModel>>()
            .Setup(v => v.ValidateAsync(model.Search, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await Sut.ApplyProjectClosuresFilters(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ShouldNotBeNull();
        redirectResult.ActionName.ShouldBe(nameof(AuthorisationsProjectClosuresController.ProjectClosures));
        redirectResult.RouteValues.ShouldContainKey("sponsorOrganisationUserId");
        redirectResult.RouteValues["sponsorOrganisationUserId"].ShouldBe(model.SponsorOrganisationUserId);
    }
}