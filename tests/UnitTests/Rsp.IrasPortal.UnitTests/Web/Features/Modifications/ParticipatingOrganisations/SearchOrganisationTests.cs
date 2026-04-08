using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.ParticipatingOrganisations.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ParticipatingOrganisations;

public class SearchOrganisationTests : TestServiceBase<ParticipatingOrganisationsController>
{
    [Fact]
    public async Task SearchOrganisation_Post_ReturnsView_WithValidationErrors()
    {
        // Arrange
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Post;
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var model = new SearchOrganisationViewModel
        {
            Search = new OrganisationSearchModel
            {
                SearchNameTerm = "ab"
            }
        };

        Mocker
            .GetMock<IValidator<SearchOrganisationViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SearchOrganisationViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure(nameof(model.Search.SearchNameTerm), "Provide 3 or more characters to search")]));

        // Act
        var result = await Sut.SearchOrganisation(model: model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SearchOrganisation");

        var returnedModel = viewResult.Model.ShouldBeOfType<SearchOrganisationViewModel>();
        returnedModel.Search.SearchNameTerm.ShouldBe("ab");

        Sut.ModelState.ContainsKey(nameof(model.Search.SearchNameTerm)).ShouldBeTrue();
        Sut.ModelState[nameof(model.Search.SearchNameTerm)]!.Errors.ShouldContain(e => e.ErrorMessage == "Provide 3 or more characters to search");
    }

    [Fact]
    public async Task SaveSelection_WithSaveForLaterTrue_And_ReviseAndAuthorise_RedirectsToSwsModifications()
    {
        // Arrange
        var http = new DefaultHttpContext();
        http.Items[ContextItemKeys.UserId] = Guid.NewGuid().ToString();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };

        var sponsorId = Guid.NewGuid();
        var modificationChangeId = Guid.NewGuid();

        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "PRJ-123",
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChangeId,
            [TempDataKeys.ProjectModification.ProjectModificationStatus] = ModificationStatus.ReviseAndAuthorise,
            [TempDataKeys.RevisionSponsorOrganisationUserId] = sponsorId,
            [TempDataKeys.RevisionRtsId] = "RTS-1"
        };

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationParticipatingOrganisations(It.IsAny<List<ParticipatingOrganisationDto>>()))
            .ReturnsAsync(new ServiceResponse().WithStatus());

        var model = new SearchOrganisationViewModel
        {
            Organisations =
            [
                new SelectableOrganisationViewModel
                {
                    IsSelected = true,
                    Organisation = new OrganisationModel { Id = "org-1" }
                }
            ]
        };

        // Act
        var result = await Sut.ConfirmSelection(model, saveForLater: true);

        // Assert
        Sut.TempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldNotBeNull();
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldBeOfType<Guid>();

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("sws:modifications");
        redirect.RouteValues.ShouldNotBeNull();
        redirect.RouteValues!["sponsorOrganisationUserId"].ShouldBe(sponsorId);
    }

    [Fact]
    public async Task SaveSelection_WithSaveForLaterTrue_RedirectsToPostApproval_AndSetsTempDataFlags()
    {
        // Arrange
        var http = new DefaultHttpContext();
        http.Items[ContextItemKeys.UserId] = Guid.NewGuid().ToString();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };

        const string projectRecordId = "PRJ-123";
        var modificationChangeId = Guid.NewGuid();

        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId,
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChangeId
        };

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationParticipatingOrganisations(It.IsAny<List<ParticipatingOrganisationDto>>()))
            .ReturnsAsync(new ServiceResponse().WithStatus());

        var model = new SearchOrganisationViewModel
        {
            Organisations =
            [
                new SelectableOrganisationViewModel
                {
                    IsSelected = true,
                    Organisation = new OrganisationModel { Id = "org-1" }
                }
            ]
        };

        // Act
        var result = await Sut.ConfirmSelection(model, saveForLater: true);

        // Assert
        Sut.TempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldNotBeNull();
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldBeOfType<Guid>();

        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pov:postapproval");
        redirectResult.RouteValues.ShouldNotBeNull();
        redirectResult.RouteValues!["projectRecordId"].ShouldBe(projectRecordId);
        redirectResult.RouteValues.ContainsKey("sponsorOrganisationUserId").ShouldBeFalse();
    }

    [Fact]
    public async Task SaveSelection_WithSaveForLaterFalse_RedirectsToParticipatingOrganisation()
    {
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };

        const string projectRecordId = "PRJ-123";
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        // Act
        var result = await Sut.ConfirmSelection(new(), saveForLater: false);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SearchOrganisation");
        Sut.ModelState.ContainsKey("participating-organisations").ShouldBeTrue();
    }

    [Fact]
    public async Task SaveSelection_WithSaveForLaterTrue_SetsNotificationBannerAndChangeMarker()
    {
        // Arrange
        var http = new DefaultHttpContext();
        http.Items[ContextItemKeys.UserId] = Guid.NewGuid().ToString();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };

        const string projectRecordId = "PRJ-123";
        var tempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId,
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid()
        };

        Sut.TempData = tempData;

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationParticipatingOrganisations(It.IsAny<List<ParticipatingOrganisationDto>>()))
            .ReturnsAsync(new ServiceResponse().WithStatus());

        var model = new SearchOrganisationViewModel
        {
            Organisations =
            [
                new SelectableOrganisationViewModel
                {
                    IsSelected = true,
                    Organisation = new OrganisationModel { Id = "org-1" }
                }
            ]
        };

        // Act
        await Sut.ConfirmSelection(model, saveForLater: true);

        // Assert
        tempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        tempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldNotBeNull();
        tempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldBeOfType<Guid>();
    }
}