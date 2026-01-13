using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ApplicationsServiceTests;

public class GetPaginatedApplicationstTests : TestServiceBase<ApplicationsService>
{
    private readonly Mock<IApplicationsServiceClient> _applicationsServiceClient;

    public GetPaginatedApplicationstTests()
    {
        _applicationsServiceClient = Mocker.GetMock<IApplicationsServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetPaginatedApplications_Should_Return_Success_Response_When_Client_Returns_Success
    (
        ProjectRecordSearchRequest searchQuery,
        int pageIndex,
        int? pageSize,
        string? sortField,
        string? sortDirection
    )
    {
        // Arrange
        var paginatedResponse = new PaginatedResponse<CompleteProjectRecordResponse>
        {
            Items = [],
            TotalCount = 0
        };

        var apiResponse = new ApiResponse<PaginatedResponse<CompleteProjectRecordResponse>>(
            new HttpResponseMessage(HttpStatusCode.OK),
            paginatedResponse,
            new RefitSettings()
        );

        _applicationsServiceClient
            .Setup(c => c.GetPaginatedApplications(searchQuery, pageIndex, pageSize, sortField, sortDirection))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetPaginatedApplications(searchQuery, pageIndex, pageSize, sortField, sortDirection);

        // Assert
        result.ShouldBeOfType<ServiceResponse<PaginatedResponse<CompleteProjectRecordResponse>>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldNotBeNull();
        result.Content!.TotalCount.ShouldBe(0);

        // Verify
        _applicationsServiceClient.Verify(c =>
            c.GetPaginatedApplications(searchQuery, pageIndex, pageSize, sortField, sortDirection), Times.Once);
    }

    [Theory, AutoData]
    public async Task GetPaginatedApplicationsByRespondent_Should_Return_Failure_Response_When_Client_Returns_Failure
    (
        ProjectRecordSearchRequest searchQuery,
        int pageIndex,
        int? pageSize,
        string? sortField,
        string? sortDirection
    )
    {
        // Arrange
        var apiResponse = new ApiResponse<PaginatedResponse<CompleteProjectRecordResponse>>(
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            null,
            new RefitSettings()
        );

        _applicationsServiceClient
            .Setup(c => c.GetPaginatedApplications(searchQuery, pageIndex, pageSize, sortField, sortDirection))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetPaginatedApplications(searchQuery, pageIndex, pageSize, sortField, sortDirection);

        // Assert
        result.ShouldBeOfType<ServiceResponse<PaginatedResponse<CompleteProjectRecordResponse>>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        result.Content.ShouldBeNull();

        // Verify
        _applicationsServiceClient.Verify(c =>
            c.GetPaginatedApplications(searchQuery, pageIndex, pageSize, sortField, sortDirection), Times.Once);
    }
}