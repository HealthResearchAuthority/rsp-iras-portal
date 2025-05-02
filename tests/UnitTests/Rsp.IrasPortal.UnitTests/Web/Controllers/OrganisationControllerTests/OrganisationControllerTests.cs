using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.OrganisationControllerTests;

public class OrganisationControllerTests : TestServiceBase<OrganisationController>
{
    [Fact]
    public async Task GetOrganisations_ShouldReturnOk_WhenServiceReturnsSuccess()
    {
        // Arrange
        var name = "TestOrg";
        var role = "TestRole";
        var organisations = new[]
        {
            new OrganisationSearchResponse { Id = "1", Name = "TestOrg1" },
            new OrganisationSearchResponse { Id = "2", Name = "TestOrg2" }
        };

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisations(name, role))
            .ReturnsAsync
            (
                new ServiceResponse<IEnumerable<OrganisationSearchResponse>>()
                    .WithContent(organisations, HttpStatusCode.OK)
            );

        // Act
        var result = await Sut.GetOrganisations(name, role, null);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(organisations.Select(o => o.Name));
    }

    [Fact]
    public async Task GetOrganisations_ShouldReturnServiceError_WhenServiceFails()
    {
        // Arrange
        var name = "TestOrg";
        var role = "TestRole";

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisations(name, role))
            .ReturnsAsync
            (
                new ServiceResponse<IEnumerable<OrganisationSearchResponse>>()
                    .WithError("Error", "Service failed", HttpStatusCode.InternalServerError)
            );

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/GetOrganisations";

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.GetOrganisations(name, role, null);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var problemDetails = viewResult.Model.ShouldBeOfType<ProblemDetails>();
        problemDetails?.Status.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetOrganisation_ShouldReturnOk_WhenServiceReturnsSuccess()
    {
        // Arrange
        var id = "1";
        var organisation = new OrganisationSearchResponse { Id = id, Name = "Org1" };

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(id))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>()
                .WithContent(organisation, HttpStatusCode.OK));

        // Act
        var result = await Sut.GetOrganisation(id);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(organisation);
    }

    [Fact]
    public async Task GetOrganisation_ShouldReturnServiceError_WhenServiceFails()
    {
        // Arrange
        var id = "1";

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(id))
            .ReturnsAsync
            (
                new ServiceResponse<OrganisationSearchResponse>()
                    .WithError("Error", "Service failed", HttpStatusCode.InternalServerError)
            );

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/GetOrganisation";

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.GetOrganisation(id);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var problemDetails = viewResult.Model.ShouldBeOfType<ProblemDetails>();
        problemDetails?.Status.ShouldBe((int)HttpStatusCode.InternalServerError);
    }
}