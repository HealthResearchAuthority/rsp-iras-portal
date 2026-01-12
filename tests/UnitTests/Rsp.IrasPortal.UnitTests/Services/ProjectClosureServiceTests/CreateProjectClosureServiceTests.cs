using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectClosureServiceTests;

public class CreateProjectClosureServiceTests : TestServiceBase<ProjectClosuresService>
{
    [Theory, AutoData]
    public async Task Should_Return_Success_Response_When_ProjectClosureRecord_Created(ProjectClosureRequest projectClosuresRequest, ProjectClosuresResponse closuresResponse)
    {
        // Arrange
        var apiResponse = new ApiResponse<ProjectClosuresResponse>(
            new HttpResponseMessage(HttpStatusCode.OK),
            closuresResponse,
            new());

        var clientMock = Mocker.GetMock<IProjectClosuresServiceClient>();

        clientMock
            .Setup(c => c.CreateProjectClosure(projectClosuresRequest))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateProjectClosure(projectClosuresRequest);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ProjectClosuresResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(closuresResponse);

        // Verify
        clientMock.Verify(c => c.CreateProjectClosure(projectClosuresRequest), Times.Once);
    }

    [Theory, AutoData]
    public async Task Should_Return_Success_Response_When_GetProjectClosureRecord_Returns_Result(string projectRecordId, ProjectClosuresSearchResponse closuresResponse)
    {
        // Arrange
        var apiResponse = new ApiResponse<ProjectClosuresSearchResponse>(
            new HttpResponseMessage(HttpStatusCode.OK),
            closuresResponse,
            new());

        var clientMock = Mocker.GetMock<IProjectClosuresServiceClient>();

        clientMock
            .Setup(c => c.GetProjectClosuresByProjectRecordId(projectRecordId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetProjectClosuresByProjectRecordId(projectRecordId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ProjectClosuresSearchResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ProjectClosures.ShouldBe(closuresResponse.ProjectClosures);

        // Verify
        clientMock.Verify(c => c.GetProjectClosuresByProjectRecordId(projectRecordId), Times.Once);
    }
}