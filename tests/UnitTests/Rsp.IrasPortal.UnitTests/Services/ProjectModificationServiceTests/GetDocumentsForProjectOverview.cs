﻿using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationServiceTests;

public class GetDocumentsForProjectOverview : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task GetDocumentsForProjectOverview_Should_Return_Success_Response_When_Client_Returns_Success(
        string projectRecordId,
        ProjectOverviewDocumentSearchRequest searchQuery,
        ProjectOverviewDocumentResponse modificationsResponse)
    {
        // Arrange
        var apiResponse = new ApiResponse<ProjectOverviewDocumentResponse>(
            new HttpResponseMessage(HttpStatusCode.OK),
            modificationsResponse,
            new());

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.GetDocumentsForProjectOverview(projectRecordId, searchQuery, 1, 20, "DocumentType", "desc"))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetDocumentsForProjectOverview(projectRecordId, searchQuery);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ProjectOverviewDocumentResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(modificationsResponse);

        // Verify
        projectModificationsServiceClient.Verify(c => c.GetDocumentsForProjectOverview(projectRecordId, searchQuery, 1, 20, "DocumentType", "desc"), Times.Once());
    }
}