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
}