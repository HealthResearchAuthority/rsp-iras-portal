using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.OrganisationControllerTests;

public class OrganisationControllerTests : TestServiceBase<OrganisationController>
{
    [Fact]
    public async Task GetOrganisationsByName_ShouldReturnOk_WhenServiceReturnsSuccess()
    {
        // Arrange
        var name = "TestOrg";
        var role = "TestRole";
        int pageIndex = 1;
        int? pageSize = null;

        var searchResponse = new OrganisationSearchResponse
        {
            Organisations = [
                new() { Id = "1", Name = "TestOrg1" },
                new() { Id = "2", Name = "TestOrg2" }],
            TotalCount = 2
        };

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(name, role, pageIndex, pageSize,null,"asc", "name"))
            .ReturnsAsync
            (
                new ServiceResponse<OrganisationSearchResponse>()
                    .WithContent(searchResponse, HttpStatusCode.OK)
            );

        // Act
        var result = await Sut.GetOrganisationsByName(name, role, pageIndex, pageSize);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(searchResponse);
    }

    [Fact]
    public async Task GetOrganisationsByName_ShouldReturnServiceError_WhenServiceFails()
    {
        // Arrange
        var name = "TestOrg";
        var role = "TestRole";
        int pageIndex = 1;
        int? pageSize = null;

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(name, role, pageIndex, pageSize,null, "asc", "name"))
            .ReturnsAsync
            (
                new ServiceResponse<OrganisationSearchResponse>()
                    .WithError("Error", "Service failed", HttpStatusCode.InternalServerError)
            );

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/GetOrganisations";

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.GetOrganisationsByName(name, role, pageIndex, pageSize);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task GetOrganisations_ShouldReturnOk_WhenServiceReturnsSuccess()
    {
        // Arrange
        var name = "TestOrg";
        var role = "TestRole";
        int pageIndex = 1;
        int? pageSize = null;

        var searchResponse = new OrganisationSearchResponse
        {
            Organisations = [
                new() { Id = "1", Name = "TestOrg1" },
                new() { Id = "2", Name = "TestOrg2" }],
            TotalCount = 2
        };

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisations(role, pageIndex, pageSize, null, "asc", "name"))
            .ReturnsAsync
            (
                new ServiceResponse<OrganisationSearchResponse>()
                    .WithContent(searchResponse, HttpStatusCode.OK)
            );

        // Act
        var result = await Sut.GetOrganisations(role, pageIndex, pageSize);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(searchResponse);
    }

    [Fact]
    public async Task GetOrganisations_ShouldReturnServiceError_WhenServiceFails()
    {
        // Arrange
        var name = "TestOrg";
        var role = "TestRole";
        int pageIndex = 1;
        int? pageSize = null;

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisations(role, pageIndex, pageSize, null, "asc", "name"))
            .ReturnsAsync
            (
                new ServiceResponse<OrganisationSearchResponse>()
                    .WithError("Error", "Service failed", HttpStatusCode.InternalServerError)
            );

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/GetOrganisations";

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.GetOrganisations(role, pageIndex, pageSize);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task GetOrganisation_ShouldReturnOk_WhenServiceReturnsSuccess()
    {
        // OrganisationSearchResponse?ange
        var id = "1";
        var organisation = new OrganisationDto { Id = id, Name = "Org1" };

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(id))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>()
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
                new ServiceResponse<OrganisationDto>()
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
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}