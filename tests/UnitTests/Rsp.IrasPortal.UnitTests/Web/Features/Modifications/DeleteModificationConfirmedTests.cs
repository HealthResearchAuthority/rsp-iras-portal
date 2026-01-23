using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications;
using Xunit.Sdk;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications;

public class DeleteModificationConfirmedTests : TestServiceBase<ModificationsController>
{
    private readonly Mock<IBlobStorageService> _blobService;
    private readonly Mock<IProjectModificationsService> _modsService;
    private readonly Mock<IRespondentService> _respService;

    public DeleteModificationConfirmedTests()
    {
        _modsService = Mocker.GetMock<IProjectModificationsService>();
        _respService = Mocker.GetMock<IRespondentService>();
        _blobService = Mocker.GetMock<IBlobStorageService>();
    }

    [Theory]
    [AutoData]
    public async Task DeleteModificationConfirmed_Should_Redirect_With_Banner_When_Service_Succeeds(
        string projectRecordId, string containerName)
    {
        // Arrange
        var projectModificationId = Guid.NewGuid();
        var projectModificationChangeId = Guid.NewGuid();
        var projectModificationIdentifier = "90000/1";

        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        _modsService
            .Setup(s => s.DeleteModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        _modsService
            .Setup(x => x.GetModificationChanges(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>
            {
                Content = new List<ProjectModificationChangeResponse>
                {
                    new()
                    {
                        Id = projectModificationChangeId
                    }
                },
                StatusCode = HttpStatusCode.OK
            });

        _respService
            .Setup(x => x.GetModificationChangesDocuments(projectModificationChangeId, projectRecordId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                Content = new List<ProjectModificationDocumentRequest>
                {
                    new()
                    {
                        ProjectRecordId = projectRecordId,
                        ProjectModificationId = projectModificationChangeId,
                        DocumentStoragePath = "IRAS/TEST.PDF"
                    }
                },
                StatusCode = HttpStatusCode.OK
            });

        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(Mock.Of<BlobContainerInfo>(), null!));
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(containerName))
            .Returns(containerClientMock.Object);

        _blobService
            .Setup(x => x.DeleteFileAsync(blobServiceClientMock.Object, "staging", "IRAS/TEST.PDF"))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var result =
            await Sut.DeleteModificationConfirmed(projectRecordId, projectModificationId,
                projectModificationIdentifier);

        // Assert: redirect and route values
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:index");
        redirect.RouteValues.ShouldNotBeNull();
        redirect.RouteValues!["projectRecordId"].ShouldBe(projectRecordId);
        redirect.RouteValues!["modificationId"].ShouldBe(projectModificationIdentifier); // uses identifier, not Guid

        // Assert: TempData flags set
        Sut.TempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldBe(projectModificationId);

        // No ProblemDetails for success path
        http.Items.ContainsKey(ContextItemKeys.ProblemDetails).ShouldBeFalse();

        // Verify service interaction
        _modsService.Verify(s => s.DeleteModification(projectRecordId, projectModificationId), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task DeleteModificationConfirmed_Should_Fail_When_NoModification(
        string projectRecordId, string containerName)
    {
        // Arrange
        var projectModificationId = Guid.NewGuid();
        var projectModificationChangeId = Guid.NewGuid();
        var projectModificationIdentifier = "90000/1";

        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        _modsService
            .Setup(s => s.DeleteModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        _modsService
            .Setup(x => x.GetModificationChanges(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>
            {
                Content = new List<ProjectModificationChangeResponse>
                {
                    new()
                    {
                        Id = projectModificationChangeId
                    }
                },
                StatusCode = HttpStatusCode.BadGateway
            });

        _respService
            .Setup(x => x.GetModificationChangesDocuments(projectModificationChangeId, projectRecordId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                Content = new List<ProjectModificationDocumentRequest>
                {
                    new()
                    {
                        ProjectRecordId = projectRecordId,
                        ProjectModificationId = projectModificationChangeId,
                        DocumentStoragePath = "IRAS/TEST.PDF"
                    }
                },
                StatusCode = HttpStatusCode.OK
            });

        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(Mock.Of<BlobContainerInfo>(), null!));
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(containerName))
            .Returns(containerClientMock.Object);

        _blobService
            .Setup(x => x.DeleteFileAsync(blobServiceClientMock.Object, "staging", "IRAS/TEST.PDF"))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var result =
            await Sut.DeleteModificationConfirmed(projectRecordId, projectModificationId,
                projectModificationIdentifier);

        // Assert: correct status
        AssertStatusCode(result, StatusCodes.Status502BadGateway);

        // Assert: TempData NOT set on failure
        Sut.TempData.ContainsKey(TempDataKeys.ShowNotificationBanner).ShouldBeFalse();
        Sut.TempData.ContainsKey(TempDataKeys.ProjectModification.ProjectModificationChangeMarker).ShouldBeFalse();
    }

    [Theory]
    [AutoData]
    public async Task DeleteModificationConfirmed_Should_Fail_When_NoModificationDocuments(
        string projectRecordId, string containerName)
    {
        // Arrange
        var projectModificationId = Guid.NewGuid();
        var projectModificationChangeId = Guid.NewGuid();
        var projectModificationIdentifier = "90000/1";

        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        _modsService
            .Setup(s => s.DeleteModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        _modsService
            .Setup(x => x.GetModificationChanges(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>
            {
                Content = new List<ProjectModificationChangeResponse>
                {
                    new()
                    {
                        Id = projectModificationChangeId
                    }
                },
                StatusCode = HttpStatusCode.OK
            });

        _respService
            .Setup(x => x.GetModificationChangesDocuments(projectModificationChangeId, projectRecordId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                Content = new List<ProjectModificationDocumentRequest>
                {
                    new()
                    {
                        ProjectRecordId = projectRecordId,
                        ProjectModificationId = projectModificationChangeId,
                        DocumentStoragePath = "IRAS/TEST.PDF"
                    }
                },
                StatusCode = HttpStatusCode.BadGateway
            });

        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(Mock.Of<BlobContainerInfo>(), null!));
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(containerName))
            .Returns(containerClientMock.Object);

        _blobService
            .Setup(x => x.DeleteFileAsync(blobServiceClientMock.Object, "staging", "IRAS/TEST.PDF"))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var result =
            await Sut.DeleteModificationConfirmed(projectRecordId, projectModificationId, projectModificationIdentifier);

        // Assert: correct status
        AssertStatusCode(result, StatusCodes.Status502BadGateway);

        // Assert: TempData NOT set on failure
        Sut.TempData.ContainsKey(TempDataKeys.ShowNotificationBanner).ShouldBeFalse();
        Sut.TempData.ContainsKey(TempDataKeys.ProjectModification.ProjectModificationChangeMarker).ShouldBeFalse();
    }

    [Theory]
    [AutoData]
    public async Task DeleteModificationConfirmed_Should_Fail_When_DeleteModification(
    string projectRecordId, string containerName)
    {
        // Arrange
        var projectModificationId = Guid.NewGuid();
        var projectModificationChangeId = Guid.NewGuid();
        var projectModificationIdentifier = "90000/1";

        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        _modsService
            .Setup(s => s.DeleteModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.BadGateway });

        _modsService
            .Setup(x => x.GetModificationChanges(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>
            {
                Content = new List<ProjectModificationChangeResponse>
                {
                    new()
                    {
                        Id = projectModificationChangeId
                    }
                },
                StatusCode = HttpStatusCode.OK
            });

        _respService
            .Setup(x => x.GetModificationChangesDocuments(projectModificationChangeId, projectRecordId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                Content = new List<ProjectModificationDocumentRequest>
                {
                    new()
                    {
                        ProjectRecordId = projectRecordId,
                        ProjectModificationId = projectModificationChangeId,
                        DocumentStoragePath = "IRAS/TEST.PDF"
                    }
                },
                StatusCode = HttpStatusCode.OK
            });

        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(Mock.Of<BlobContainerInfo>(), null!));
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(containerName))
            .Returns(containerClientMock.Object);

        _blobService
            .Setup(x => x.DeleteFileAsync(blobServiceClientMock.Object, "staging", "IRAS/TEST.PDF"))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var result =
            await Sut.DeleteModificationConfirmed(projectRecordId, projectModificationId,
                projectModificationIdentifier);

        // Assert: correct status
        AssertStatusCode(result, StatusCodes.Status502BadGateway);

        // Assert: TempData NOT set on failure
        Sut.TempData.ContainsKey(TempDataKeys.ShowNotificationBanner).ShouldBeFalse();
        Sut.TempData.ContainsKey(TempDataKeys.ProjectModification.ProjectModificationChangeMarker).ShouldBeFalse();
    }

    /// <summary>
    ///     Helper to assert status code regardless of whether ServiceError returns StatusCodeResult or ObjectResult.
    /// </summary>
    private static void AssertStatusCode(IActionResult result, int expected)
    {
        if (result is StatusCodeResult sc)
        {
            sc.StatusCode.ShouldBe(expected);
            return;
        }

        if (result is ObjectResult oc && oc.StatusCode.HasValue)
        {
            oc.StatusCode.Value.ShouldBe(expected);
            return;
        }

        throw new XunitException($"Unexpected result type: {result.GetType().Name}");
    }
}