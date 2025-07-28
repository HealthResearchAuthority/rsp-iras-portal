using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.RtsServiceTests;

public class RtsServiceTests : TestServiceBase<RtsService>
{
    private readonly Mock<IRtsServiceClient> _rtsServiceClient;

    public RtsServiceTests()
    {
        _rtsServiceClient = Mocker.GetMock<IRtsServiceClient>();
    }

    [Fact]
    public async Task GetOrganisation_ShouldReturnOrganisation_WhenIdIsValid()
    {
        // Arrange
        var organisationId = "123";
        var apiResponse = new ApiResponse<OrganisationDto>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new OrganisationDto { Id = organisationId, Name = "Test Organisation" },
            new()
        );

        Mocker
            .GetMock<IRtsServiceClient>()
            .Setup(client => client.GetOrganisation(organisationId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetOrganisation(organisationId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<OrganisationDto>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.Id.ShouldBe(organisationId);
        result.Content.Name.ShouldBe("Test Organisation");

        // Verify
        _rtsServiceClient.Verify(client => client.GetOrganisation(organisationId), Times.Once());
    }

    [Fact]
    public async Task GetOrganisationsByName_ShouldReturnListOfOrganisations_WhenNameIsValid()
    {
        // Arrange
        var organisationName = "Test";
        var expectedResponse = new ApiResponse<OrganisationSearchResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new()
            {
                Organisations = [
                    new() { Id = "123", Name = "Test Organisation 1" },
                    new() { Id = "456", Name = "Test Organisation 2" }],
                TotalCount = 2
            },
            new()
        );

        Mocker
            .GetMock<IRtsServiceClient>()
            .Setup(client => client.GetOrganisationsByName(organisationName, null, null, null))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await Sut.GetOrganisationsByName(organisationName, null, null, null);

        // Assert
        result.ShouldBeOfType<ServiceResponse<OrganisationSearchResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content.Organisations.Count.ShouldBe(2);
        result.Content.Organisations.ShouldContain(o => o.Name.Contains("Test Organisation"));
    }

    [Fact]
    public async Task GetOrganisationsByName_WithPagination_ShouldReturnPaginatedListOfOrganisations()
    {
        // Arrange
        var organisationName = "Test";
        var pageIndex = 1;
        var pageSize = 2;
        var expectedResponse = new ApiResponse<OrganisationSearchResponse>
       (
          new HttpResponseMessage(HttpStatusCode.OK),
           new()
           {
               Organisations = [
                    new() { Id = "123", Name = "Test Organisation 1" },
                    new() { Id = "456", Name = "Test Organisation 2" }],
               TotalCount = 2
           },
            new()
       );

        Mocker
            .GetMock<IRtsServiceClient>()
            .Setup(client => client.GetOrganisationsByName(organisationName, null, pageIndex, pageSize))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await Sut.GetOrganisationsByName(organisationName, null, pageIndex, pageSize);

        // Assert
        result.ShouldBeOfType<ServiceResponse<OrganisationSearchResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content.Organisations.Count.ShouldBe(2);
        result.Content.Organisations.ShouldContain(o => o.Name.Contains("Test Organisation"));
    }

    [Fact]
    public async Task GetOrganisations_ShouldReturnListOfOrganisations_WhenNameIsValid()
    {
        // Arrange
        var expectedResponse = new ApiResponse<OrganisationSearchResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new()
            {
                Organisations = [
                    new() { Id = "123", Name = "Test Organisation 1" },
                    new() { Id = "456", Name = "Test Organisation 2" }],
                TotalCount = 2
            },
            new()
        );

        Mocker
            .GetMock<IRtsServiceClient>()
            .Setup(client => client.GetOrganisations(null, null, null))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await Sut.GetOrganisations(null, null, null);

        // Assert
        result.ShouldBeOfType<ServiceResponse<OrganisationSearchResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content.Organisations.Count.ShouldBe(2);
        result.Content.Organisations.ShouldContain(o => o.Name.Contains("Test Organisation"));
    }

    [Fact]
    public async Task GetOrganisations_WithPagination_ShouldReturnPaginatedListOfOrganisations()
    {
        // Arrange
        var pageIndex = 1;
        var pageSize = 2;
        var expectedResponse = new ApiResponse<OrganisationSearchResponse>
       (
          new HttpResponseMessage(HttpStatusCode.OK),
           new()
           {
               Organisations = [
                    new() { Id = "123", Name = "Test Organisation 1" },
                    new() { Id = "456", Name = "Test Organisation 2" }],
               TotalCount = 2
           },
            new()
       );

        Mocker
            .GetMock<IRtsServiceClient>()
            .Setup(client => client.GetOrganisations(null, pageIndex, pageSize))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await Sut.GetOrganisations(null, pageIndex, pageSize);

        // Assert
        result.ShouldBeOfType<ServiceResponse<OrganisationSearchResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content.Organisations.Count.ShouldBe(2);
        result.Content.Organisations.ShouldContain(o => o.Name.Contains("Test Organisation"));
    }
}