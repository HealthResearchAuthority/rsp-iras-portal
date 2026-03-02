using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationServiceTests;

public class GetProjectDocumentsAuditTrailTests : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task GetProjectDocumentsAuditTrail_DelegatesToClient_AndReturnsMappedResult
    (
        string projectRecordId,
        int pageNumber,
        int pageSize,
        string sortField,
        string sortDirection
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<ProjectDocumentsAuditTrailResponse>(
            httpResponseMessage,
            null,
            new(),
            null);

        var client = Mocker.GetMock<IProjectModificationsServiceClient>();

        client
            .Setup(c => c.GetProjectDocumentsAuditTrail
            (
                projectRecordId,
                pageNumber,
                pageSize,
                sortField,
                sortDirection
            ))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetProjectDocumentsAuditTrail
        (
            projectRecordId,
            pageNumber,
            pageSize,
            sortField,
            sortDirection
        );

        // Assert
        client.Verify(c =>
            c.GetProjectDocumentsAuditTrail(projectRecordId, pageNumber, pageSize, sortField, sortDirection),
            Times.Once);

        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}