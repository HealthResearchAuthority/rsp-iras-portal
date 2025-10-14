using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ApplicationsServiceTests;

public class GetPaginatedApplicationsByRespondentTests : TestServiceBase<ApplicationsService>
{
    private readonly Mock<IApplicationsServiceClient> _applicationsServiceClient;

    public GetPaginatedApplicationsByRespondentTests()
    {
        _applicationsServiceClient = Mocker.GetMock<IApplicationsServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetPaginatedApplicationsByRespondent_Should_Return_Success_Response_When_Client_Returns_Success
    (
        string respondentId,
        ApplicationSearchRequest searchQuery,
        int pageIndex,
        int? pageSize,
        string? sortField,
        string? sortDirection
    )
    {
        // Arrange
        var paginatedResponse = new PaginatedResponse<IrasApplicationResponse>
        {
            Items = [],
            TotalCount = 0
        };

        var apiResponse = new ApiResponse<PaginatedResponse<IrasApplicationResponse>>(
            new HttpResponseMessage(HttpStatusCode.OK),
            paginatedResponse,
            new RefitSettings()
        );

        _applicationsServiceClient
            .Setup(c => c.GetPaginatedApplicationsByRespondent(respondentId, searchQuery, pageIndex, pageSize, sortField, sortDirection))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetPaginatedApplicationsByRespondent(respondentId, searchQuery, pageIndex, pageSize, sortField, sortDirection);

        // Assert
        result.ShouldBeOfType<ServiceResponse<PaginatedResponse<IrasApplicationResponse>>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldNotBeNull();
        result.Content!.TotalCount.ShouldBe(0);

        // Verify
        _applicationsServiceClient.Verify(c =>
            c.GetPaginatedApplicationsByRespondent(respondentId, searchQuery, pageIndex, pageSize, sortField, sortDirection), Times.Once);
    }

    [Theory, AutoData]
    public async Task GetPaginatedApplicationsByRespondent_Should_Return_Failure_Response_When_Client_Returns_Failure
    (
        string respondentId,
        ApplicationSearchRequest searchQuery,
        int pageIndex,
        int? pageSize,
        string? sortField,
        string? sortDirection
    )
    {
        // Arrange
        var apiResponse = new ApiResponse<PaginatedResponse<IrasApplicationResponse>>(
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            null,
            new RefitSettings()
        );

        _applicationsServiceClient
            .Setup(c => c.GetPaginatedApplicationsByRespondent(respondentId, searchQuery, pageIndex, pageSize, sortField, sortDirection))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetPaginatedApplicationsByRespondent(respondentId, searchQuery, pageIndex, pageSize, sortField, sortDirection);

        // Assert
        result.ShouldBeOfType<ServiceResponse<PaginatedResponse<IrasApplicationResponse>>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        result.Content.ShouldBeNull();

        // Verify
        _applicationsServiceClient.Verify(c =>
            c.GetPaginatedApplicationsByRespondent(respondentId, searchQuery, pageIndex, pageSize, sortField, sortDirection), Times.Once);
    }
}