using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.Documents.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Documents;

public class DeleteDocumentsTests : TestServiceBase<DocumentsController>
{
    [Theory, AutoData]
    public async Task DeleteDocuments_Should_Redirect_When_No_Documents(
        ModificationDeleteDocumentViewModel model)
    {
        // Arrange
        model.Documents = null;

        // Act
        var result = await Sut.DeleteDocuments(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(DocumentsController.AddDocumentDetailsList));
    }

    [Theory, AutoData]
    public async Task DeleteDocuments_Should_Redirect_To_ProjectDocument_When_Single_Delete_And_No_Documents_Remain(
        ProjectModificationDocumentRequest doc,
        string containerName,
        string projectRecordId,
        Guid changeId,
        string respondentId)
    {
        // Arrange
        var model = new ModificationDeleteDocumentViewModel
        {
            Documents = new List<ProjectModificationDocumentRequest> { doc }
        };

        var serviceResponse = new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
        {
            Content = new List<ProjectModificationDocumentRequest>(), // no documents left
            StatusCode = HttpStatusCode.OK
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.DeleteDocumentModification(It.IsAny<List<ProjectModificationDocumentRequest>>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

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

        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DeleteFileAsync(blobServiceClientMock.Object, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(changeId, projectRecordId, respondentId))
            .ReturnsAsync(serviceResponse);

        SetupControllerContext(changeId, projectRecordId, respondentId);

        // Act
        var result = await Sut.DeleteDocuments(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(DocumentsController.ProjectDocument));

        Mocker.GetMock<IProjectModificationsService>().Verify(
            s => s.DeleteDocumentModification(It.IsAny<List<ProjectModificationDocumentRequest>>()), Times.Once);

        Mocker.GetMock<IRespondentService>().Verify(
            s => s.GetModificationChangesDocuments(changeId, projectRecordId, respondentId), Times.Once);
    }

    [Theory, AutoData]
    public async Task DeleteDocuments_Should_Redirect_To_AddDocumentDetailsList_When_Single_Delete_And_Documents_Remain(
        ProjectModificationDocumentRequest doc,
        string projectRecordId,
        Guid changeId,
        string respondentId,
        string containerName)
    {
        // Arrange
        var model = new ModificationDeleteDocumentViewModel
        {
            Documents = new List<ProjectModificationDocumentRequest> { doc }
        };

        var serviceResponse = new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
        {
            Content = new List<ProjectModificationDocumentRequest>
            {
                new ProjectModificationDocumentRequest { Id = Guid.NewGuid(), FileName = "test.pdf" }
            },
            StatusCode = HttpStatusCode.OK
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.DeleteDocumentModification(It.IsAny<List<ProjectModificationDocumentRequest>>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

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

        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DeleteFileAsync(blobServiceClientMock.Object, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(changeId, projectRecordId, respondentId))
            .ReturnsAsync(serviceResponse);

        SetupControllerContext(changeId, projectRecordId, respondentId);

        // Act
        var result = await Sut.DeleteDocuments(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(DocumentsController.AddDocumentDetailsList));
    }

    [Theory, AutoData]
    public async Task DeleteDocuments_Should_Redirect_To_ProjectDocument_When_Multiple_Delete(
        List<ProjectModificationDocumentRequest> docs,
        string projectRecordId,
        Guid changeId,
        string respondentId,
        string containerName)
    {
        // Arrange
        var model = new ModificationDeleteDocumentViewModel { Documents = docs };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.DeleteDocumentModification(It.IsAny<List<ProjectModificationDocumentRequest>>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

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

        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DeleteFileAsync(blobServiceClientMock.Object, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        SetupControllerContext(changeId, projectRecordId, respondentId);

        // Act
        var result = await Sut.DeleteDocuments(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(DocumentsController.ProjectDocument));

        Mocker.GetMock<IProjectModificationsService>().Verify(
            s => s.DeleteDocumentModification(It.IsAny<List<ProjectModificationDocumentRequest>>()), Times.Once);
    }

    private void SetupControllerContext(Guid changeId, string projectRecordId, string respondentId)
    {
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = changeId,
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { [ContextItemKeys.UserId] = respondentId }
            }
        };
    }
}