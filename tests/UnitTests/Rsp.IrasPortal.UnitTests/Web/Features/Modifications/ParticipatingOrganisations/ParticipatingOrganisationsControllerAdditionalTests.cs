using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.ParticipatingOrganisations.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ParticipatingOrganisations;

public class ParticipatingOrganisationsControllerAdditionalTests : TestServiceBase<ParticipatingOrganisationsController>
{
    [Fact]
    public async Task SelectedParticipatingOrganisations_MapsOrganisationIdAndStoresTempData()
    {
        // Arrange
        var modificationChangeId = Guid.NewGuid();
        var participatingOrganisationId = Guid.NewGuid();
        var http = new DefaultHttpContext();

        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "PR-1",
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChangeId
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationParticipatingOrganisations(modificationChangeId, "PR-1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ParticipatingOrganisationDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new ParticipatingOrganisationDto
                    {
                        Id = participatingOrganisationId,
                        OrganisationId = "ORG-1"
                    }
                ]
            });

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation("ORG-1"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Id = "ORG-1", Name = "Org Name" }
            });

        // Act
        var result = await Sut.SelectedParticipatingOrganisations();

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SelectedOrganisationsViewModel>();
        model.SelectedOrganisations.Count.ShouldBe(1);
        model.SelectedOrganisations[0].Id.ShouldBe("ORG-1");
        model.SelectedOrganisations[0].OrganisationId.ShouldBe(participatingOrganisationId);

        var json = Sut.TempData.Peek(TempDataKeys.ProjectModification.SelectedParticipatingOrganisations) as string;
        json.ShouldNotBeNull();
        JsonSerializer.Deserialize<List<ParticipatingOrganisationModel>>(json!).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeselectOrganisation_WithRedirectToReview_RedirectsToReviewRoute()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var specificAreaOfChangeId = Guid.NewGuid();
        var modificationChangeId = Guid.NewGuid();

        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "PR-1",
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeId] = specificAreaOfChangeId,
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChangeId
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.DeleteModificationParticipatingOrganisation(organisationId))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.DeselectOrganisation(organisationId, redirectToReview: true);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:reviewchanges");
        redirect.RouteValues!["projectRecordId"].ShouldBe("PR-1");
        redirect.RouteValues["specificAreaOfChangeId"].ShouldBe(specificAreaOfChangeId.ToString());
        redirect.RouteValues["modificationChangeId"].ShouldBe(modificationChangeId.ToString());
        redirect.RouteValues["reviseChange"].ShouldBe(bool.TrueString);
    }

    [Fact]
    public async Task DeselectOrganisation_WithoutReviewRedirect_RedirectsToSelectedParticipatingOrganisations()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var http = new DefaultHttpContext();

        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.DeleteModificationParticipatingOrganisation(organisationId))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.DeselectOrganisation(organisationId, redirectToReview: false);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(ParticipatingOrganisationsController.SelectedParticipatingOrganisations));
    }

    [Fact]
    public async Task DeselectOrganisation_WhenDeleteFails_ReturnsServiceErrorStatusCode()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var http = new DefaultHttpContext();

        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.DeleteModificationParticipatingOrganisation(organisationId))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.BadRequest, Error = "failed" });

        // Act
        var result = await Sut.DeselectOrganisation(organisationId);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task RemoveParticipatingOrganisation_ReturnsView_WithOrganisationTupleModel()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        const string organisationName = "Org to remove";

        // Act
        var result = await Sut.RemoveParticipatingOrganisation(organisationId, organisationName);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<ValueTuple<Guid, string>>();
        model.Item1.ShouldBe(organisationId);
        model.Item2.ShouldBe(organisationName);
    }

    [Fact]
    public void ClearFilters_WhenSearchModelExists_ClearsFilterCollectionsAndRedirects()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };

        var searchModel = new OrganisationSearchModel
        {
            SearchNameTerm = "hospital",
            Countries = ["England", "Wales"],
            OrganisationTypes = ["NHS"],
            OrganisationStatuses = ["Active organisations"]
        };

        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.OrganisationSearchModel] = JsonSerializer.Serialize(searchModel)
        };

        // Act
        var result = Sut.ClearFilters();

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("porgs:searchorganisation");

        var updatedJson = Sut.TempData.Peek(TempDataKeys.OrganisationSearchModel) as string;
        updatedJson.ShouldNotBeNull();

        var updated = JsonSerializer.Deserialize<OrganisationSearchModel>(updatedJson!);
        updated.ShouldNotBeNull();
        updated!.SearchNameTerm.ShouldBe("hospital");
        updated.Countries.ShouldBeEmpty();
        updated.OrganisationTypes.ShouldBeEmpty();
        updated.OrganisationStatuses.ShouldBeEmpty();
    }

    [Fact]
    public void ClearFilters_WhenNoSearchModelExists_StillRedirects()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.ClearFilters();

        // Assert
        result.ShouldBeOfType<RedirectToRouteResult>().RouteName.ShouldBe("porgs:searchorganisation");
    }

    [Fact]
    public void RemoveFilter_WhenSearchModelMissing_RedirectsToParticipatingOrganisations()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.RemoveFilter(OrganisationSearch.CountryKey, "England");

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(ParticipatingOrganisationsController.ParticipatingOrganisations));
    }

    [Theory]
    [InlineData(OrganisationSearch.CountryKey, "england")]
    [InlineData(OrganisationSearch.OrganisationTypeKey, "nhs")]
    [InlineData(OrganisationSearch.OrganisationStatusKey, "active organisations")]
    public void RemoveFilter_WhenValueExists_RemovesItCaseInsensitive_AndRedirects(string key, string valueToRemove)
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };

        var searchModel = new OrganisationSearchModel
        {
            Countries = ["England", "Wales"],
            OrganisationTypes = ["NHS", "Private"],
            OrganisationStatuses = ["Active organisations", "Terminated organisations"]
        };

        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.OrganisationSearchModel] = JsonSerializer.Serialize(searchModel)
        };

        // Act
        var result = Sut.RemoveFilter(key, valueToRemove);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("porgs:searchorganisation");

        var updatedJson = Sut.TempData.Peek(TempDataKeys.OrganisationSearchModel) as string;
        var updated = JsonSerializer.Deserialize<OrganisationSearchModel>(updatedJson!);

        updated.ShouldNotBeNull();

        switch (key)
        {
            case OrganisationSearch.CountryKey:
                updated!.Countries.ShouldNotContain("England");
                updated.Countries.ShouldContain("Wales");
                break;

            case OrganisationSearch.OrganisationTypeKey:
                updated!.OrganisationTypes.ShouldNotContain("NHS");
                updated.OrganisationTypes.ShouldContain("Private");
                break;

            case OrganisationSearch.OrganisationStatusKey:
                updated!.OrganisationStatuses.ShouldNotContain("Active organisations");
                updated.OrganisationStatuses.ShouldContain("Terminated organisations");
                break;
        }
    }

    [Fact]
    public async Task SelectedParticipatingOrganisations_WhenModificationParticipatingOrganisationsLookupFails_ReturnsServiceErrorStatusCode()
    {
        // Arrange
        var modificationChangeId = Guid.NewGuid();
        var http = new DefaultHttpContext();

        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "PR-1",
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChangeId
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationParticipatingOrganisations(modificationChangeId, "PR-1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ParticipatingOrganisationDto>>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = "failed"
            });

        // Act
        var result = await Sut.SelectedParticipatingOrganisations();

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task SelectedParticipatingOrganisations_WhenOrganisationLookupFails_ReturnsServiceErrorStatusCode()
    {
        // Arrange
        var modificationChangeId = Guid.NewGuid();
        var participatingOrganisationId = Guid.NewGuid();
        var http = new DefaultHttpContext();

        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "PR-1",
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChangeId
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationParticipatingOrganisations(modificationChangeId, "PR-1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ParticipatingOrganisationDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new ParticipatingOrganisationDto
                    {
                        Id = participatingOrganisationId,
                        OrganisationId = "ORG-1"
                    }
                ]
            });

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation("ORG-1"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = "failed"
            });

        // Act
        var result = await Sut.SelectedParticipatingOrganisations();

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }
}